using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Em3Emulator
{

    // формат данных: 8, 16, 32
    // Способ аддресации: НА, ПА, БРА, ОА
    //
    // Регистровая память: 
    //      Количество: 32
    //      Тип: Универсальная

    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        static void testFillTable()
        {
        }
    }
}
