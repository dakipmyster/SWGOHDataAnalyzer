using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWGOHDBInterface;
using SWGOHMessage;

namespace SWGOHReportBuilder
{
    public class ReportBuilder
    {
        
        DataBuilder m_dataBuilder;

        public ReportBuilder()
        {
            m_dataBuilder = new DataBuilder();
        }

        public async Task BuildReport()
        {
            m_dataBuilder.GetSnapshotNames();

            await m_dataBuilder.CollectPlayerGPDifferences();
        }

        public bool CanRunReport()
        {
            return m_dataBuilder.CanRunReport();
        }
    }
}
