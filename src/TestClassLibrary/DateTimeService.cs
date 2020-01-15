using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClassLibrary
{
    public class DateTimeService
    {
        public DateTime GetCurrentDateTime()
        {
            DateTime now = DateTime.UtcNow;

            now.AddDays(-7);

            return now;
        }
    }
}
