using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWGOHReportBuilder
{
    public static class HTMLConstructor
    {
        public static string TableGroupStart(bool noBorder = false)
        {
            StringBuilder sb = new StringBuilder();

            if(noBorder)
                sb.AppendLine("<table border=\"0\">");
            else
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
                sb.AppendLine("<tr>");
                sb.AppendLine($"<th colspan= \"{headers.Length.ToString()}\">{tableLabel}</th>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("<tr>");
            foreach(string header in headers)            
                sb.AppendLine($"<th>{header}</th>");
            
            sb.AppendLine("</tr>");
            sb.AppendLine(tableData);
            sb.AppendLine("</table>");            

            return sb.ToString();
        }
    }
}
