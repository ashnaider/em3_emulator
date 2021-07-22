using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Em3Emulator
{
    /*
     * 
     */
    class Em3Core
    {
        public static int AddInt(int lhs, int rhs)
        {
            return lhs + rhs;
        }

        public static float AddFloat(float lhs, float rhs)
        {
            return lhs + rhs;
        }

        public static int SubInt(int lhs, int rhs)
        {
            return lhs - rhs;
        }

        public static float SubFloat(float lhs, float rhs)
        {
            return lhs - rhs;
        }

        public static int MulInt(int lhs, int rhs)
        {
            return lhs * rhs;
        }

        public static float MulFloat(float lhs, float rhs)
        {
            return lhs * rhs;
        }

        public static int DivInt(int lhs, int rhs)
        {
            return lhs / rhs;
        }

        public static float DivFloat(float lhs, float rhs)
        {
            return lhs / rhs;
        }

        public static int Modulo(int lhs, int rhs)
        {
            return lhs % rhs;
        }

    }
}
