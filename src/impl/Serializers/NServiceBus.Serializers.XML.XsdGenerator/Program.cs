using System;
using System.IO;
using System.Text;
using System.Reflection;

namespace NServiceBus.Serializers.XML.XsdGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Assembly a = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, args[0]));

            Events.GuidDetected += delegate
                                       {
                                           needToGenerateGuid = true;
                                       };

            foreach(Type t in a.GetTypes())
                TopLevelScan(t);

            string xsd = GenerateXsdString();

            using(StreamWriter writer = File.CreateText(GetFileName()))
                writer.Write(xsd);

            if (needToGenerateGuid)
                using (StreamWriter writer = File.CreateText(GetFileName()))
                    writer.Write(Strings.GuidXsd);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Usage: first parameter [required], your assembly.");
        }

        private static string GetFileName()
        {
            int i = 0;
            while (File.Exists(string.Format("schema{0}.xsd", i)))
                i++;

            return string.Format("schema{0}.xsd", i);
        }

        private static string GenerateXsdString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            builder.AppendLine("<xs:schema elementFormDefault=\"qualified\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">");

            if (needToGenerateGuid)
                builder.AppendLine("<xs:import namespace=\"http://microsoft.com/wsdl/types/\" />");

            foreach (ComplexType complex in Repository.ComplexTypes)
            {
                builder.AppendFormat("<xs:element name=\"{0}\" nillable=\"true\" type=\"{0}\" />\n", complex.Name);
                builder.Append(ComplexTypeWriter.Write(complex));
            }

            foreach (Type simple in Repository.SimpleTypes)
            {
                builder.AppendFormat("<xs:element name=\"{0}\" type=\"{0}\" />\n", simple.Name);
                builder.Append(SimpleTypeWriter.Write(simple));
            }

            builder.AppendLine("</xs:schema>");

            string result = builder.ToString();

            return result;
        }

        public static void TopLevelScan(Type type)
        {
            if (typeof(IMessage).IsAssignableFrom(type))
                Scan(type);
        }

        public static void Scan(Type type)
        {
            if (type == null || type == typeof(object) || type == typeof(IMessage))
                return;

            Repository.Handle(type);

            if (!type.IsInterface)
                Scan(type.BaseType);
            else
                foreach (Type i in type.GetInterfaces())
                    Scan(i);
        }

        private static bool needToGenerateGuid;
    }
}