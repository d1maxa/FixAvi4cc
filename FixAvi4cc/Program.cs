using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixAvi4cc
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var fixer = new Fixer(args);
                fixer.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
