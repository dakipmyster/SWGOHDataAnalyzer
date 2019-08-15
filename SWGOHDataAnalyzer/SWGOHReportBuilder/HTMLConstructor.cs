using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWGOHReportBuilder
{
    public static class HTMLConstructor
    {
        public static string TableGroupStart()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<table>");
            sb.AppendLine("<tr valign=\"top\">");
            sb.AppendLine("<td>");

            return sb.ToString();
        }

        public static string TableGroupEnd()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("</td>");
            sb.AppendLine("</tr>");
            sb.AppendLine("</table>");

            return sb.ToString();
        }

        public static string TableGroupNext()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("</td>");
            sb.AppendLine("<td>");

            return sb.ToString();
        }

        public static string AddTable(string[] headers, string tableData, string tableLabel = null)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<table>");

            if (!String.IsNullOrEmpty(tableLabel))
            {                
                sb.AppendLine("<tr style=\"border: 1px solid black\" bgcolor =\"yellow\">");
                sb.AppendLine($"<th style=\"border: 1px solid black\" colspan=\"{headers.Length.ToString()}\">{tableLabel}</th>");
                sb.AppendLine("</tr>");
                sb.AppendLine("<tr/>");
            }

            sb.AppendLine("<tr style=\"border: 1px solid black\" bgcolor=\"orange\">");
            foreach (string header in headers)            
                sb.AppendLine($"<th style=\"border: 1px solid black\">{header}</th>");
            
            sb.AppendLine("</tr>");
            sb.AppendLine(tableData);
            sb.AppendLine("</table>");            

            return sb.ToString();
        }

        public static string AddTableData(string[] dataValues)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<tr style=\"border: 1px solid black\">");

            foreach (string data in dataValues)
                sb.AppendLine($"<td style=\"border: 1px solid black\">{data}</td>");

            sb.AppendLine("</tr>");

            return sb.ToString();
        }

        public static string SectionHeader(string headerText)
        {
            return $"<h1><center>{headerText}</center></h1>";
        }
    }
}
