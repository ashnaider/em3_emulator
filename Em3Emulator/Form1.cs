using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.VisualBasic;
using System.IO;

namespace Em3Emulator
{


    public partial class Form1 : Form
    {
        private const int EM3_memory_size = 512;
        private const int num_base = EM3_memory_size;
        private const int rows_count = EM3_memory_size;
        private const int cols_count = 5;

        private const int Command_col = 1;
        private const int A1_col = 2;
        private const int A2_col = 3;
        private const int A3_col = 4;

        private const int R1_row = 0;
        private const int R2_row = 1;
        private const int S_row  = 2;
        private const int RK_row = 3;


        private int curr_pos = 0;
        private int prev_pos = 0;
        private int iteration_count = 0;
        private const int MAX_ITERATIONS = 5000;

        private const double EPS = 0.0001;

        private int Omega_register = 0;

        private bool prog_started = false;
        private bool debug_started = false;
        private bool continue_debugging = false;
        private bool prog_crashed = false;

        private Dictionary<long, String> IntToCommands = new Dictionary<long, String>();
        private Dictionary<String, long> CommandsToInt = new Dictionary<String, long>();


        private HashSet<int> FloatRows = new HashSet<int>();
        private HashSet<int> IntRows = new HashSet<int>();

        private HashSet<int> FloatRows_registerTable = new HashSet<int>();
        private HashSet<int> IntRows_registerTable = new HashSet<int>();

        private String curr_path_to_save = String.Empty;

        public Form1()
        {
            InitializeComponent();
            SetUpTable();
            SetUpWidgets();
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            IntToCommands[0]  = "ПЕР";
            IntToCommands[1]  = "СЛВ";
            IntToCommands[2]  = "ВЧВ";
            IntToCommands[3]  = "УМВ";
            IntToCommands[4]  = "ДЕВ";
            IntToCommands[5]  = "ВВВ";
            IntToCommands[6]  = "ВВЦ";
            IntToCommands[7]  = "???";
            IntToCommands[8]  = "???";
            IntToCommands[9]  = "БЕЗ";
            IntToCommands[10] = "ЦЕЛ";
            IntToCommands[11] = "СЛЦ";
            IntToCommands[12] = "ВЧЦ";
            IntToCommands[13] = "УМЦ";
            IntToCommands[14] = "ДЕЦ";
            IntToCommands[15] = "ВЫВ";
            IntToCommands[16] = "ВЫЦ";
            IntToCommands[17] = "???";
            IntToCommands[18] = "???";
            IntToCommands[19] = "УСЛ";
            IntToCommands[20] = "ВЕЩ";
            IntToCommands[21] = "???";
            IntToCommands[22] = "СЛС";
            IntToCommands[23] = "???";
            IntToCommands[24] = "МОД";
            IntToCommands[25] = "???";
            IntToCommands[26] = "???";
            IntToCommands[27] = "???";
            IntToCommands[28] = "???";
            IntToCommands[29] = "???";
            IntToCommands[30] = "???";
            IntToCommands[31] = "ОСТ";

            CommandsToInt["ПЕР"] = 0;
            CommandsToInt["СЛВ"] = 1;
            CommandsToInt["ВЧВ"] = 3;
            CommandsToInt["УМВ"] = 3;
            CommandsToInt["ДЕВ"] = 4;
            CommandsToInt["ВВВ"] = 5;
            CommandsToInt["ВВЦ"] = 6;
            CommandsToInt["???"] = 7;
            CommandsToInt["???"] = 8;
            CommandsToInt["БЕЗ"] = 9;
            CommandsToInt["ЦЕЛ"] = 10;
            CommandsToInt["СЛЦ"] = 11;
            CommandsToInt["ВЧЦ"] = 12;
            CommandsToInt["УМЦ"] = 13;
            CommandsToInt["ДЕЦ"] = 14;
            CommandsToInt["ВЫВ"] = 15;
            CommandsToInt["ВЫЦ"] = 16;
            CommandsToInt["???"] = 17;
            CommandsToInt["???"] = 18;
            CommandsToInt["УСЛ"] = 19;
            CommandsToInt["ВЕЩ"] = 20;
            CommandsToInt["???"] = 21;
            CommandsToInt["СЛС"] = 22;
            CommandsToInt["???"] = 23;
            CommandsToInt["МОД"] = 24;
            CommandsToInt["???"] = 25;
            CommandsToInt["???"] = 26;
            CommandsToInt["???"] = 27;
            CommandsToInt["???"] = 28;
            CommandsToInt["???"] = 29;
            CommandsToInt["???"] = 30;
            CommandsToInt["ОСТ"] = 31;
        }

        private void SetUpWidgets()
        {
            entered_data_textBox.BackColor = System.Drawing.SystemColors.Window;
            output_textBox.BackColor = System.Drawing.SystemColors.Window;
            error_textBox.BackColor = System.Drawing.SystemColors.Window;

            step_button.Enabled = false;
            stop_button.Enabled = false;
        }

        private void SetUpTable()
        {
            CodeTable.AllowUserToDeleteRows    = false;
            CodeTable.AllowUserToResizeColumns = false;
            CodeTable.AllowUserToResizeRows    = false;
            CodeTable.AllowUserToOrderColumns  = false;

            foreach (DataGridViewColumn column in CodeTable.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            registers_dataGridView.AllowUserToDeleteRows    = false;
            registers_dataGridView.AllowUserToResizeColumns = false;
            registers_dataGridView.AllowUserToResizeRows    = false;
            registers_dataGridView.AllowUserToOrderColumns  = false;

            foreach (DataGridViewColumn column in registers_dataGridView.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            CodeTable.CellPainting += CodeTable_CellPainting;
            registers_dataGridView.CellPainting += registerTable_CellPainting;

            FillEmptyTable();

            FillEmptyRegistersTable();

            for (int i = 0; i < cols_count; ++i)
            {
                // TODO: create adjustable col width
                CodeTable.Columns[i].Width = 100;
                ((DataGridViewTextBoxColumn)CodeTable.Columns[i]).MaxInputLength = 3;
            }

            CodeTable.Columns[0].ReadOnly = true;
        }

        private void SetCodeTableReadOnly(bool val)
        {
            CodeTable.Columns[Command_col].ReadOnly = val;
            CodeTable.Columns[A1_col].ReadOnly = val;
            CodeTable.Columns[A2_col].ReadOnly = val;
            CodeTable.Columns[A3_col].ReadOnly = val;

        }

        private void FillEmptyTable()
        {
            for (int i = 0; i < rows_count; ++i)
            {
                CodeTable.Rows.Add(String.Format("{0}", i+1), "ПЕР", "000", "000", "000");
            }
        }

        private void FillEmptyRegistersTable()
        {
            registers_dataGridView.Rows.Add("R1", "ПЕР", "000", "000", "000");
            registers_dataGridView.Rows.Add("R2", "ПЕР", "000", "000", "000");
            registers_dataGridView.Rows.Add("S",  "ПЕР", "000", "000", "000");
            registers_dataGridView.Rows.Add("RK", "ПЕР", "000", "000", "000");
        }

        private void ClearTable()
        {
            for (int i = 0; i < rows_count; ++i)
            {
                CodeTable.Rows[i].Cells[Command_col].Value = "ПЕР";
                CodeTable.Rows[i].Cells[A1_col].Value = "000";
                CodeTable.Rows[i].Cells[A2_col].Value = "000";
                CodeTable.Rows[i].Cells[A3_col].Value = "000";
            }
        }

        private bool AllowedToChangeCode()
        {
            if (prog_started || debug_started)
            {
                return false;
            }
            return true;
        }

        private void SetRowReadOnly(int row_index, bool val)
        {
            CheckAddress(row_index);
            CodeTable.Rows[row_index].ReadOnly = val;
        }

        private void SetRowToCode(int row_index)
        {

            if (IntRows.Contains(row_index))
            {
                IntRows.Remove(row_index);
            }
            if (FloatRows.Contains(row_index))
            {
                FloatRows.Remove(row_index);
            }

            FillRowBy(row_index, "ПЕР", "000", "000", "000");
            SetRowReadOnly(row_index, false);
        }

        private void SetRowToCodeByUser()
        {
            if (!AllowedToChangeCode())
            {
                return;
            }

            int selected_cell_index = GetSelectedCellIndex();
            if (selected_cell_index == -1)
            {
                MessageBox.Show("Сначала выберите строку, для записи кода");
            }
            else
            {
                SetRowToCode(selected_cell_index);
            }
        }

        private void FillRowBy(int address, string comm, string a1, string a2, string a3)
        {
            CheckAddress(address);
            CodeTable.Rows[address].Cells[Command_col].Value = comm;
            CodeTable.Rows[address].Cells[A1_col].Value = a1;
            CodeTable.Rows[address].Cells[A2_col].Value = a2;
            CodeTable.Rows[address].Cells[A3_col].Value = a3;
        }

        private int GetSelectedCellIndex()
        {
            int selected_cell_index = CodeTable.CurrentCell.RowIndex;

            return selected_cell_index;
        }

        private String GetCellValue(int i, int j)
        {
            if (i < 0 || j < 0 || i >= rows_count || j >= cols_count)
            {
                throw new IndexOutOfRangeException();
            }

            String res = "none";

            if (CodeTable.Rows.Count > i && CodeTable.Rows[i] != null)
            {
                if (CodeTable.Rows[i].Cells.Count > j &&   CodeTable.Rows[i].Cells[j] != null)
                {
                    res = CodeTable.Rows[i].Cells[j].Value.ToString();
                }
            }
            
            return res;
        }

        private void GetIntArgs(int address, out int A1_int, out int A2_int, out int A3_int)
        {
            CheckAddress(address);
            String A1 = GetCellValue(address, A1_col);
            String A2 = GetCellValue(address, A2_col);
            String A3 = GetCellValue(address, A3_col);

            A1 = (A1.Trim() == string.Empty) ? "0" : A1;
            A2 = (A2.Trim() == string.Empty) ? "0" : A2;
            A3 = (A3.Trim() == string.Empty) ? "0" : A3;

            float A1_float, A2_float, A3_float;

            if (!float.TryParse(A1, out A1_float) ||
                !float.TryParse(A2, out A2_float) ||
                !float.TryParse(A3, out A3_float))
            {
                throw new ArgumentException("Wrong arguments!");
            }

            A1_int = (int)A1_float;
            A2_int = (int)A2_float;
            A3_int = (int)A3_float;
        }

        private bool CheckAddress(int address)
        {
            if (address < 0 || address >= rows_count)
            {
                MessageBox.Show("Неправильный адресс!");
                throw new ArgumentException();
            }
            return true;
        }

        private bool CheckRegisterTabelAddress(int address)
        {
            if (address < 0 || address >= 4)
            {
                MessageBox.Show("Неправильный адресс для записи в регистр!");
                throw new ArgumentException();
            }
            return true;
        }

        private int ReadInt(int address)
        {
            CheckAddress(address);

            String int_str = CodeTable.Rows[address].Cells[A1_col].Value.ToString();
            int int_res;

            Debug.Print(String.Format("Address to read int: {0}", address));
            Debug.Print("Hash set contains: ");
            foreach (int row in IntRows)
            {
                Debug.Print(String.Format("\t {0}", row));
            }


            if (!IntRows.Contains(address))
            {
                //throw new ArgumentException("Невозможно прочитать целое число.");
                String comm = CodeTable.Rows[address].Cells[Command_col].Value.ToString();
                if (comm == "ПЕР")
                {
                    return 0;
                }

            }

            if (!int.TryParse(int_str, out int_res))
            {
                throw new ArgumentException("Невозможно прочитать целое число.");

            }

            return int_res;
        }

        private float ReadFloat(int address)
        {
            // TODO: normal read float from table
            CheckAddress(address);

            String float_str = CodeTable.Rows[address].Cells[A1_col].Value.ToString();
            float float_res;

            Debug.Print(String.Format("Address to read float: {0}", address));
            Debug.Print("Hash set contains: ");
            foreach (int row in FloatRows)
            {
                Debug.Print(String.Format("\t {0}", row));
            }
            
            
            if (!FloatRows.Contains(address) )
            {
                //throw new ArgumentException("Невозможно прочитать вещественное число.");
                String comm = CodeTable.Rows[address].Cells[Command_col].Value.ToString();
                if (comm == "ПЕР")
                {
                    return 0;
                }
            }

            if (!float.TryParse(float_str, out float_res))
            {
                throw new ArgumentException("Невозможно прочитать вещественное число.");

            }

            return float_res;
        }

        private void WriteInt(int address, int val)
        {
            CheckAddress(address);

            IntRows.Add(address);

            CodeTable.Rows[address].Cells[Command_col].Value = "INT";
            CodeTable.Rows[address].Cells[A1_col].Value = val.ToString();
            CodeTable.Rows[address].Cells[A2_col].Value = "";
            CodeTable.Rows[address].Cells[A3_col].Value = "";

            //CodeTable.CellPainting += CodeTable_CellPainting;
        }

        private void WriteRegisterInt(int address, int val)
        {
            CheckRegisterTabelAddress(address);

            IntRows_registerTable.Add(address);

            registers_dataGridView.Rows[address].Cells[Command_col].Value = "INT";
            registers_dataGridView.Rows[address].Cells[A1_col].Value = val.ToString();
            registers_dataGridView.Rows[address].Cells[A2_col].Value = "";
            registers_dataGridView.Rows[address].Cells[A3_col].Value = "";
            
            registers_dataGridView.CellPainting += registerTable_CellPainting;
        }

        private void WriteFloat(int address, float val)
        {
            CheckAddress(address);

            FloatRows.Add(address);

            CodeTable.Rows[address].Cells[Command_col].Value = "FLOAT";
            CodeTable.Rows[address].Cells[A1_col].Value = val.ToString();
            CodeTable.Rows[address].Cells[A2_col].Value = "";
            CodeTable.Rows[address].Cells[A3_col].Value = "";

            CodeTable.CellPainting += CodeTable_CellPainting;
        }

        private void WriteRegisterFloat(int address, float val)
        {
            CheckRegisterTabelAddress(address);

            FloatRows_registerTable.Add(address);

            registers_dataGridView.Rows[address].Cells[Command_col].Value = "FLOAT";
            registers_dataGridView.Rows[address].Cells[A1_col].Value = val.ToString();
            registers_dataGridView.Rows[address].Cells[A2_col].Value = "";
            registers_dataGridView.Rows[address].Cells[A3_col].Value = "";

            registers_dataGridView.CellPainting += registerTable_CellPainting;
        }

        private void WriteIntByUser()
        {
            if (!AllowedToChangeCode())
            {
                return;
            }

            int selected_cell = GetSelectedCellIndex();
            if (selected_cell == -1)
            {
                MessageBox.Show("Сначала выберите строку для записи числа");
            }
            else
            {
                try
                {
                    GetInt(selected_cell, 1);
                    SetRowReadOnly(selected_cell, true);
                }
                catch (ArgumentException exc)
                {

                }
            }
        }

        private void GetInt(int address, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                String str_num = Interaction.InputBox("Введите целое число: ");
                int int_num;
                if (!int.TryParse(str_num, out int_num))
                {
                    MessageBox.Show("Введено некоректное значение");
                    throw new ArgumentException();
                }

                WriteInt(address + i, int_num);

                if (prog_started || debug_started)
                {
                    entered_data_textBox.AppendText(int_num.ToString());
                    entered_data_textBox.AppendText(Environment.NewLine);
                }
            }
        }

        private void WriteFloatByUser()
        {
            if (!AllowedToChangeCode())
            {
                return;
            }

            int selected_cell = GetSelectedCellIndex();
            if (selected_cell == -1)
            {
                MessageBox.Show("Сначала выберите строку для записи числа");
            }
            else
            {
                GetFloat(selected_cell, 1);
                SetRowReadOnly(selected_cell, true);
            }
        }

        private void GetFloat(int address, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                String str_num = Interaction.InputBox("Введите вещественное число: ");
                str_num = str_num.Replace('.', ',');
                float float_num;
                if (!float.TryParse(str_num, out float_num))
                {
                    MessageBox.Show("Введено некоректное значение");
                    throw new ArgumentException();
                }

                WriteFloat(address + i, float_num);

                if (prog_started || debug_started)
                {
                    entered_data_textBox.AppendText(float_num.ToString());
                    entered_data_textBox.AppendText(Environment.NewLine);
                }
            }
        }

        private void PrintInt(int address, int count)
        {
            CheckAddress(address);
            CheckAddress(address + count);

            for (int i = 0; i < count; ++i)
            {
                output_textBox.AppendText(ReadInt(address).ToString());
                output_textBox.AppendText(Environment.NewLine);
            }
        }

        private void PrintFloat(int address, int count)
        {
            CheckAddress(address);
            CheckAddress(address + count);

            for (int i = 0; i < count; ++i)
            {
                output_textBox.AppendText(ReadFloat(address).ToString());
                output_textBox.AppendText(Environment.NewLine);
            }
        }

        private void SetOmega(int val)
        {
            if (val < 0 || val > 2)
            {
                throw new ArgumentException("Неверное значение для <омега>");
            }
            Omega_register = val;
            omega_register_label.Text = "w: " + val.ToString();
        }

        private void SetNextRow_RA(int address)
        {
            CheckAddress(address);

            RA_register_label.Text = "RA: " + address.ToString().PadLeft(3, '0');
        }

        private void SetRegisters_R1_R2_S_RK(Row R1, Row R2, Row S, Row RK)
        {
            /*они показывают переменные над которыми производится команда, 
             * R1 = A2, R2 = A3, S = A1
             * RK это строка на которой стоит сейчас программа*/

            if (R1.Command == "FLOAT")
            {
                float val = ReadFloat(R1.Address);
                WriteRegisterFloat(R1_row, val);
            } 
            else if (R1.Command == "INT")
            {
                WriteRegisterInt(R1_row, R1.A1);
            }
            else
            {
                registers_dataGridView.Rows[R1_row].Cells[Command_col].Value = R1.Command;
                registers_dataGridView.Rows[R1_row].Cells[A1_col].Value = R1.A1.ToString().PadLeft(3, '0');
                registers_dataGridView.Rows[R1_row].Cells[A2_col].Value = R1.A2.ToString().PadLeft(3, '0');
                registers_dataGridView.Rows[R1_row].Cells[A3_col].Value = R1.A3.ToString().PadLeft(3, '0');
            }

            if (R2.Command == "FLOAT")
            {
                float val = ReadFloat(R2.Address);
                WriteRegisterFloat(R2_row, val);
            }
            else if (R2.Command == "INT")
            {
                WriteRegisterInt(R2_row, R2.A1);
            }
            else
            {
                registers_dataGridView.Rows[R2_row].Cells[Command_col].Value = R2.Command;
                registers_dataGridView.Rows[R2_row].Cells[A1_col].Value = R2.A1.ToString().PadLeft(3, '0');
                registers_dataGridView.Rows[R2_row].Cells[A2_col].Value = R2.A2.ToString().PadLeft(3, '0');
                registers_dataGridView.Rows[R2_row].Cells[A3_col].Value = R2.A3.ToString().PadLeft(3, '0');
            }

            if (S.Command == "FLOAT")
            {
                float val = ReadFloat(S.Address);
                WriteRegisterFloat(S_row, val);
            }
            else if (S.Command == "INT")
            {
                WriteRegisterInt(S_row, S.A1);
            }
            else
            {
                registers_dataGridView.Rows[S_row].Cells[Command_col].Value = S.Command;
                registers_dataGridView.Rows[S_row].Cells[A1_col].Value = S.A1.ToString().PadLeft(3, '0');
                registers_dataGridView.Rows[S_row].Cells[A2_col].Value = S.A2.ToString().PadLeft(3, '0');
                registers_dataGridView.Rows[S_row].Cells[A3_col].Value = S.A3.ToString().PadLeft(3, '0');
            }


            registers_dataGridView.Rows[RK_row].Cells[Command_col].Value = RK.Command;
            registers_dataGridView.Rows[RK_row].Cells[A1_col].Value = RK.A1.ToString().PadLeft(3, '0');
            registers_dataGridView.Rows[RK_row].Cells[A2_col].Value = RK.A2.ToString().PadLeft(3, '0');
            registers_dataGridView.Rows[RK_row].Cells[A3_col].Value = RK.A3.ToString().PadLeft(3, '0');
        }

        private void GetCheck_and_SetRegisters(Row curr_row)
        {

            if (curr_row.Command == "ВВВ" ||
                curr_row.Command == "ВВЦ" ||
                curr_row.Command == "ВЫВ" ||
                curr_row.Command == "ВЫЦ")
            {
                CheckAddress(curr_row.A1);

                Row S = GetRow(curr_row.A1);
                ++S.A1; ++S.A2; ++S.A3;
                Row R1 = new Row("ПЕР", 0, 0, curr_row.A2+1);
                Row R2 = new Row("ПЕР", 0, 0, 0);
                Row RK = new Row(curr_row.Command, curr_row.A1 + 1, curr_row.A2 + 1, curr_row.A3 + 1);
                SetRegisters_R1_R2_S_RK(R1, R2, S, RK);
            }
            else if (curr_row.Command == "INT" ||
                     curr_row.Command == "FLOAT")
            {
                MessageBox.Show("in get check and set second condition");
                Row S  = new Row("ПЕР", 0, 0, 0);
                Row R1 = new Row("ПЕР", 0, 0, 0);
                Row R2 = new Row("ПЕР", 0, 0, 0);
                Row RK = new Row(curr_row.Command, curr_row.A1, 0, 0);
                SetRegisters_R1_R2_S_RK(R1, R2, S, RK);
                
                if (curr_row.Command == "INT")
                {
                    WriteRegisterInt(RK_row, curr_row.A1);
                }
                else if (curr_row.Command == "FLOAT")
                {
                    float val = ReadFloat(curr_row.Address);
                    WriteRegisterFloat(RK_row, val);
                }
                
            }
            else
            {

                Row R1 = GetRow(curr_row.A2);
                Row R2 = GetRow(curr_row.A3);
                Row S = GetRow(curr_row.A1);

                if (curr_row.A2 != -1)
                {
                    ++R1.A1; ++R1.A2; ++R1.A3;
                }
                if (curr_row.A3 != -1)
                {
                    ++R2.A1; ++R2.A2; ++R2.A3;
                }
                if (curr_row.A1 != -1)
                {
                    ++S.A1; ++S.A2; ++S.A3;
                }

                Row RK = new Row(curr_row.Command, curr_row.A1 + 1, curr_row.A2 + 1, curr_row.A3 + 1);
                SetRegisters_R1_R2_S_RK(R1, R2, S, RK);
            }
        }
 
        private void CheckIntAndSetOmega(int res)
        {

            if (res == 0)
            {
                SetOmega(0);
            }
            else if (res < 0)
            {
                SetOmega(1);
            }
            else if (res > 0)
            {
                SetOmega(2);
            }
        }

        private void CheckFloatAndSetOmega(float res)
        {
            
            if (WithInEps(res, 0, EPS))
            {
                SetOmega(0);
            }
            else if (res < 0)
            {
                SetOmega(1);
            }
            else if (res > 0)
            {
                SetOmega(2);
            }
        }

        private void CodeTable_CellPainting(object sender,
                                        DataGridViewCellPaintingEventArgs e)
        {
            if ((FloatRows.Contains(e.RowIndex) || 
                IntRows.Contains(e.RowIndex)) && e.ColumnIndex != Command_col
                                              && e.ColumnIndex != 0)
            {
                e.AdvancedBorderStyle.Right = DataGridViewAdvancedCellBorderStyle.None;
                e.AdvancedBorderStyle.Left = DataGridViewAdvancedCellBorderStyle.None;
            }
        }

        private void registerTable_CellPainting(object sender,
                                        DataGridViewCellPaintingEventArgs e)
        {
            if ((FloatRows_registerTable.Contains(e.RowIndex) ||
                IntRows_registerTable.Contains(e.RowIndex)) && e.ColumnIndex != Command_col
                                                            && e.ColumnIndex != 0)
            {
                e.AdvancedBorderStyle.Right = DataGridViewAdvancedCellBorderStyle.None;
                e.AdvancedBorderStyle.Left = DataGridViewAdvancedCellBorderStyle.None;
            }
        }

        private bool WithInEps(float a, float b, double eps)
        {
            if (Math.Abs(a - b) < eps)
            {
                return true;
            }
            return false;
        }

        enum DType { INT, FLOAT, OTH };

        private DType GetDataType(int addr)
        {
            CheckAddress(addr);
            string str_type = GetCellValue(addr, Command_col);

            switch (str_type)
            {
                case "INT":
                    return DType.INT;
                case "FLOAT":
                    return DType.FLOAT;
                default:
                    return DType.OTH;
            }
        }

        private void TransferOp(int dest, int source)
        {
            if (dest == -1)
            {
                return;
            }

            CheckAddress(dest);

            if (source != -1)
            {
                CheckAddress(source);
                DType source_dtype = GetDataType(source);

                if (source_dtype == DType.INT)
                {
                    //MessageBox.Show("In dtype int!");

                    int res = ReadInt(source);
                    WriteInt(dest, res);
                }
                else if (source_dtype == DType.FLOAT)
                {
                    //MessageBox.Show("In dtype int!");

                    float res = ReadFloat(source);
                    WriteFloat(dest, res);
                }
            } 
            else
            {
                DType dest_dtype = GetDataType(dest);
                if (dest_dtype == DType.FLOAT)
                {
                    WriteFloat(dest, 0);
                }
                else 
                {
                    WriteInt(dest, 0);
                }
            }
        }

        private void AddIntOp(int address, int a_addr, int b_addr)
        {
            CheckAddress(address);
            CheckAddress(a_addr);
            CheckAddress(b_addr);
            

            int a, b;
            a = ReadInt(a_addr);
            b = ReadInt(b_addr);
            int res = Em3Core.AddInt(a, b);

            CheckIntAndSetOmega(res);

            WriteInt(address, res);
        }

        private void AddFloatOp(int address, int a_addr, int b_addr)
        {
            CheckAddress(address);
            CheckAddress(a_addr);
            CheckAddress(b_addr);


            float a, b;
            a = ReadFloat(a_addr);
            b = ReadFloat(b_addr);
            float res = Em3Core.AddFloat(a, b);

            CheckFloatAndSetOmega(res);

            WriteFloat(address, res);
        }

        private long ReadIntFromRow(int address)
        {
            CheckAddress(address);

            int a1_val, a2_val, a3_val;
            long comm_val;
            GetIntArgs(address, out a1_val, out a2_val, out a3_val);

            string comm_str = CodeTable.Rows[address].Cells[Command_col].Value.ToString();
            if (!long.TryParse(comm_str, out comm_val))
            {
                if (!CommandsToInt.ContainsKey(comm_str))
                {
                    throw new ArgumentException(String.Format(
                        "Неизвестное значение {0} в колонке комманды. Строка: {1}", comm_str, address + 1));
                }

                comm_val = CommandsToInt[comm_str];
            }

            Debug.Print(String.Format("Comm val: {0}", comm_val));
            long res = a3_val;
            res += a2_val * num_base;
            res += a1_val * num_base * num_base;
            res += comm_val * num_base * num_base * num_base;
            Debug.Print(String.Format("Res in print int: {0}", res));
            return res;
        }

        private void WriteIntAsRow(int address, long val)
        {
            CheckAddress(address);

            long a3_val = val % num_base;
            long a2_val = (val / num_base) % num_base;
            long a1_val = (val / (long)Math.Pow(num_base, 2)) % num_base;

            long comm_val = (val / (long)Math.Pow(num_base, 3)) % num_base;
            String comm_str = IntToCommands[comm_val];

            CodeTable.Rows[address].Cells[A1_col].Value = a1_val.ToString().PadLeft(3, '0');
            CodeTable.Rows[address].Cells[A2_col].Value = a2_val.ToString().PadLeft(3, '0');
            CodeTable.Rows[address].Cells[A3_col].Value = a3_val.ToString().PadLeft(3, '0');
            CodeTable.Rows[address].Cells[Command_col].Value = comm_str; //comm_val.ToString().PadLeft(3, '0');
        }


        private void AddRows(String command, int A1, int A2, int A3)
        {
            CheckAddress(A1);
            CheckAddress(A2);
            CheckAddress(A3);

            long lhs = ReadIntFromRow(A2);
            long rhs = ReadIntFromRow(A3);
            long res = lhs + rhs;
            //MessageBox.Show(String.Format("lhs: {0}, rhs: {1}, res: {2}", lhs, rhs, res));
            WriteIntAsRow(A1, res);
        } 

        private void SubIntOp(int address, int a_addr, int b_addr)
        {
            CheckAddress(address);
            CheckAddress(a_addr);
            CheckAddress(b_addr);


            int a, b;
            a = ReadInt(a_addr);
            b = ReadInt(b_addr);
            int res = Em3Core.SubInt(a, b);

            CheckIntAndSetOmega(res);

            WriteInt(address, res);
        }

        private void SubFloatOp(int address, int a_addr, int b_addr)
        {
            CheckAddress(address);
            CheckAddress(a_addr);
            CheckAddress(b_addr);


            float a, b;
            a = ReadFloat(a_addr);
            b = ReadFloat(b_addr);
            float res = Em3Core.SubFloat(a, b);

            CheckFloatAndSetOmega(res);

            WriteFloat(address, res);
        }

        private void MulIntOp(int address, int a_addr, int b_addr)
        {
            CheckAddress(address);
            CheckAddress(a_addr);
            CheckAddress(b_addr);


            int a, b;
            a = ReadInt(a_addr);
            b = ReadInt(b_addr);
            int res = Em3Core.MulInt(a, b);

            CheckIntAndSetOmega(res);

            WriteInt(address, res);
        }

        private void MulFloatOp(int address, int a_addr, int b_addr)
        {
            CheckAddress(address);
            CheckAddress(a_addr);
            CheckAddress(b_addr);


            float a, b;
            a = ReadFloat(a_addr);
            b = ReadFloat(b_addr);
            float res = Em3Core.MulFloat(a, b);

            CheckFloatAndSetOmega(res);

            WriteFloat(address, res);
        }

        private void DivIntOp(int address, int a_addr, int b_addr)
        {
            CheckAddress(address);
            CheckAddress(a_addr);
            CheckAddress(b_addr);


            int a, b;
            a = ReadInt(a_addr);
            b = ReadInt(b_addr);
            int res = Em3Core.DivInt(a, b);

            CheckIntAndSetOmega(res);

            WriteInt(address, res);
        }

        private void DivFloatOp(int address, int a_addr, int b_addr)
        {
            CheckAddress(address);
            CheckAddress(a_addr);
            CheckAddress(b_addr);


            float a, b;
            a = ReadFloat(a_addr);
            b = ReadFloat(b_addr);
            float res = Em3Core.DivFloat(a, b);

            CheckFloatAndSetOmega(res);

            WriteFloat(address, res);
        }

        private void GoTo(int address)
        {
            CheckAddress(address);

            curr_pos = address;
        }

        private int ConditionGoTo(int a1, int a2, int a3)
        {
            CheckAddress(a1);
            CheckAddress(a2);
            CheckAddress(a3);

            switch (Omega_register)
            {
                case 0:     // s == 0
                    return a1;

                case 1:     // s < 0
                    return a2;

                case 2:     // s > 0
                    return a3;
                default:
                    throw new ArgumentException("Недопустимое состояние регистра омега!");
            }
        }

        private void ConditionGoToOp(int a1, int a2, int a3)
        {
            curr_pos = ConditionGoTo(a1, a2, a3);
        }

        private void ConvertToInt(int dest_addr, int source_addr)
        {
            if (dest_addr == -1)
            {
                return;
            }

            CheckAddress(dest_addr);

            if (source_addr == -1)
            {
                WriteInt(dest_addr, 0);
                return;
            }

            CheckAddress(source_addr);

            string comm = GetCellValue(source_addr, Command_col);
            int source_val;
            if (comm == "INT")
            {
                source_val = ReadInt(source_addr);
            }
            else if (comm == "FLOAT")
            {
                source_val = (int)ReadFloat(source_addr);
            }
            else
            {
                source_val = (int)ReadIntFromRow(source_addr);
            }

            WriteInt(dest_addr, source_val);
        }

        private void ConvertToFloat(int dest_addr, int source_addr)
        {
            if (dest_addr == -1)
            {
                return;
            }

            CheckAddress(dest_addr);

            if (source_addr == -1)
            {
                WriteFloat(dest_addr, 0);
                return;
            }

            CheckAddress(source_addr);

            string comm = GetCellValue(source_addr, Command_col);
            float source_val;
            if (comm == "INT")
            {
                source_val = ReadInt(source_addr);
            }
            else if (comm == "FLOAT")
            {
                source_val = ReadFloat(source_addr);
            }
            else
            {
                source_val = (float)ReadIntFromRow(source_addr);
            }

            WriteFloat(dest_addr, source_val);
        }

        private int Modulo(int lhs, int rhs)
        {
            return Em3Core.Modulo(lhs, rhs);
        }

        private void ModuloOp(int A1, int A2, int A3)
        {
            CheckAddress(A1);
            CheckAddress(A2);
            CheckAddress(A3);

            int lhs = ReadInt(A2);
            int rhs = ReadInt(A3);

            int res = Modulo(lhs, rhs);

            WriteInt(A1, res);
        }

        private void SetErrRegister(int status)
        {
            if (status < 0 || status > 1)
            {
                throw new ArgumentException("Недопустимое значение регистра омега");
            }
            Error_register_label.Text = "Error: " + status.ToString();
        }

        private int GetNextPosition(Row row)
        {
            switch (row.Command.Trim())
            {
                case "БЕЗ":
                    return row.A2;
                case "УСЛ":
                    return ConditionGoTo(row.A1, row.A2, row.A3);
                case "ОСТ":
                    return -1;
                default:
                    return curr_pos + 1;
            }
        }

        private Row GetRow(int address)
        {
            Row row = new Row();

            if (address == -1)
            {
                row.Command = "ПЕР";
                row.A1 = 0;
                row.A2 = 0;
                row.A3 = 0;
                row.Address = address;
                return row;
            }

            CheckAddress(address);

            row.Command = GetCellValue(address, Command_col);

            int A1, A2, A3;
            GetIntArgs(address, out A1, out A2, out A3);

            --A1;
            --A2;
            --A3;

            row.A1 = A1;
            row.A2 = A2;
            row.A3 = A3;

            row.Address = address;

            return row;
        }

        private bool Iteration()
        {
            Row row = GetRow(curr_pos);

            int A1 = row.A1;
            int A2 = row.A2;
            int A3 = row.A3;

            bool Continue = true;
            Debug.WriteLine("Iteration {0}. Command = {1}", curr_pos, row.Command);
            switch (row.Command.Trim())
            {
                case "ПЕР":
                    TransferOp(A1, A3);
                    break;
                case "СЛВ":
                    AddFloatOp(A1, A2, A3);
                    break;
                case "ВЧВ":
                    SubFloatOp(A1, A2, A3);
                    break;
                case "УМВ":
                    MulFloatOp(A1, A2, A3);
                    break;
                case "ДЕВ":
                    DivFloatOp(A1, A2, A3);
                    break;
                case "ВВВ":
                    GetFloat(A1, A2 + 1);
                    break;
                case "ВВЦ":
                    GetInt(A1, A2 + 1);
                    break;
                case "БЕЗ":
                    GoTo(A2);
                    return true;
                case "ЦЕЛ":
                    ConvertToInt(A1, A3);  // needs tests
                    break;
                case "СЛЦ":
                    AddIntOp(A1, A2, A3);
                    break;
                case "СЛС":
                    AddRows(row.Command, A1, A2, A3);
                    break;
                case "ВЧЦ":
                    SubIntOp(A1, A2, A3);
                    break;
                case "УМЦ":
                    MulIntOp(A1, A2, A3);
                    break;
                case "ДЕЦ":
                    DivIntOp(A1, A2, A3);
                    break;
                case "ВЫВ":
                    PrintFloat(A1, A2 + 1);
                    break;
                case "ВЫЦ":
                    PrintInt(A1, A2 + 1);
                    break;
                case "УСЛ":
                    ConditionGoToOp(A1, A2, A3);
                    return true;
                case "ВЕЩ":
                    ConvertToFloat(A1, A3);  // needs tests
                    break;
                case "МОД":
                    ModuloOp(A1, A2, A3);   // needs test
                    break;
                case "ОСТ":
                    Continue = false;
                    break;
                default:
                    throw new ArgumentException(String.Format("Неизвестная команда: {0}.",
                        row.Command));
                    break;
            }

            curr_pos++;
            return Continue;
        }

        private void ResetData()
        {
            curr_pos = 0;
            iteration_count = 0;
            prev_pos = 0;
            Omega_register = 0;
            SetNextRow_RA(000);
            SetErrRegister(0);

            output_textBox.Text = string.Empty;
            entered_data_textBox.Text = string.Empty;
            error_textBox.Text = string.Empty;
        }

        private void ActivateStartButtons(bool val)
        {
            start_button.Enabled = val;
            debug_button.Enabled = val;
        }

        private void Start()
        {
            ResetData();
            SetCodeTableReadOnly(true);

            ActivateStartButtons(false);

            prog_started = true;

            bool Continue = false;
            while (true)
            {
                ++iteration_count;

                if (iteration_count >= MAX_ITERATIONS)
                {
                    break;
                }

                if (curr_pos >= rows_count)
                {
                    break;
                }


                try
                {
                    Continue = Iteration();
                if (!Continue)
                {
                    break;
                }
                }
                catch (Exception exc)
                {
                    ErrorMsg(exc.Message);
                    prog_crashed = true;
                    break;
                }
            }

            if (!prog_crashed)
            {
                SuccessMsg();
            }

            ActivateStartButtons(true);
            SetCodeTableReadOnly(false);

            prog_started = false;
        }

        private void ErrorMsg(string err_msg)
        {
            SetErrRegister(1);
            error_textBox.AppendText("Программа завершена аварийно!");
            error_textBox.AppendText(Environment.NewLine);
            error_textBox.AppendText(String.Format("Строка: {0}", curr_pos + 1));
            error_textBox.AppendText(Environment.NewLine);
            error_textBox.AppendText(err_msg);
            error_textBox.AppendText(Environment.NewLine);

            MessageBox.Show("Произошла ошибка!");
        }

        private void SuccessMsg()
        {
            error_textBox.AppendText("Программа завершена без ошибок!");
            error_textBox.AppendText(Environment.NewLine);
        }

        private void StartDebug()
        {
            ResetData();
            SetCodeTableReadOnly(true);
            ActivateStartButtons(false);
            ActivateDebugButtons(true);
            debug_started = true;
            
            continue_debugging = true;

            CodeTable.Rows[0].Selected = true;
            prev_pos = 0;

            Row curr_row = GetRow(curr_pos);
            int next_pos = GetNextPosition(curr_row);
            SetNextRow_RA(next_pos);

        }

        private void ActivateDebugButtons(bool val)
        {
            step_button.Enabled = val;
            stop_button.Enabled = val;
        }

        private void HighLightNextRow(int next_pos)
        {
            CodeTable.Rows[curr_pos].Selected = false;

            CheckAddress(next_pos);

            CodeTable.Rows[next_pos].Selected = true;
            prev_pos = next_pos;
        }

        private void DebugStep()
        {

            Row curr_row = GetRow(curr_pos);
            int next_pos = GetNextPosition(curr_row);
            
            ++iteration_count;
            if (iteration_count >= MAX_ITERATIONS ||
                curr_pos >= rows_count ||
                !continue_debugging ||
                next_pos == -1)
            {
                StopDebugging();
                return;
            }


            HighLightNextRow(next_pos);
            SetNextRow_RA(next_pos + 1);

            try
            {
                continue_debugging = Iteration();
                GetCheck_and_SetRegisters(curr_row);
            }
            catch (Exception exc)
            {
                ErrorMsg(exc.Message);
                prog_crashed = true;
            }
        }

        private void StopDebugging()
        {
            if (!prog_crashed)
            {
                SuccessMsg();
            }

            CodeTable.Rows[prev_pos].Selected = false;

            continue_debugging = false;
            debug_started = false;
            prog_crashed = false;
            ActivateDebugButtons(false);
            ActivateStartButtons(true);
            SetCodeTableReadOnly(false);
        }



        private void start_button_Click(object sender, EventArgs e)
        {
            Start();
        }

        private void debug_button_Click(object sender, EventArgs e)
        {
            StartDebug();
        }

        private void ReadFile(String fileName)
        {
            ClearTable();

            var lines = File.ReadLines(fileName);
            int i = 0;
            String comm, a1, a2, a3;
            foreach (var line in lines)
            {
                String[] operands = line.Split(' ');
                
                comm = "ПЕР";
                a1 = "000";
                a2 = "000";
                a3 = "000";

                bool isData = false;

                if (operands.Length > 0)
                {
                    comm = operands[0];

                    if (comm == "INT")
                    {
                        IntRows.Add(i);
                        isData = true;
                    }
                    else if (comm == "FLOAT")
                    {
                        FloatRows.Add(i);
                        isData = true;
                    }
                }
                if (operands.Length > 1)
                {
                    a1 = operands[1];
                }
                if (operands.Length > 2)
                {
                    a2 = operands[2];
                }
                if (operands.Length > 3)
                {
                    a3 = operands[3];
                }

                CodeTable.Rows[i].Cells[Command_col].Value = comm;
                CodeTable.Rows[i].Cells[A1_col].Value = a1;
                if (isData)
                {
                    CodeTable.Rows[i].Cells[A2_col].Value = string.Empty;
                    CodeTable.Rows[i].Cells[A3_col].Value = string.Empty;
                }
                else
                {
                    CodeTable.Rows[i].Cells[A2_col].Value = a2;
                    CodeTable.Rows[i].Cells[A3_col].Value = a3;
                }
                ++i;
            }
        }

        private void SaveFile(String fileName)
        {
            using (var sw = new StreamWriter(fileName))
            {
                String comm, a1, a2, a3;

                for (int i = 0; i < rows_count; ++i)
                {
                    comm = CodeTable.Rows[i].Cells[Command_col].Value.ToString();

                    if (comm == "INT" || comm == "FLOAT")
                    {
                        a1 = CodeTable.Rows[i].Cells[A1_col].Value.ToString();
                        a2 = string.Empty;
                        a3 = string.Empty;
                    } 
                    else
                    {
                        a1 = CodeTable.Rows[i].Cells[A1_col].Value.ToString().PadLeft(3, '0');
                        a2 = CodeTable.Rows[i].Cells[A2_col].Value.ToString().PadLeft(3, '0');
                        a3 = CodeTable.Rows[i].Cells[A3_col].Value.ToString().PadLeft(3, '0');
                    }
                    
                    sw.WriteLine(String.Format("{0} {1} {2} {3}", comm, a1, a2, a3));

                    //if (comm == "ОСТ")
                    //{
                    //    break;
                    //}
                }
            }
        }

        private void OpenFileHandler()
        {
            String fileName = String.Empty;
            using (var selectFileDialog = new OpenFileDialog())
            {
                if (selectFileDialog.ShowDialog() == DialogResult.OK)
                {
                    fileName = selectFileDialog.FileName;

                    try
                    {
                        IntRows.Clear();
                        FloatRows.Clear();
                        ReadFile(fileName);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("Невозможно прочитать файл. " + exc.Message);
                    }

                    curr_path_to_save = fileName;
                }
            }
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileHandler();
        }

        private void SaveAsHandler()
        {
            String fileName = String.Empty;
            using (var selectFileDialog = new SaveFileDialog())
            {
                selectFileDialog.Filter = "Text files (*.txt)|*.txt";
                if (selectFileDialog.ShowDialog() == DialogResult.OK)
                {
                    fileName = selectFileDialog.FileName;

                    try
                    {
                        SaveFile(fileName);
                        MessageBox.Show("Файл сохранён!");
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("Невозможно сохранить файл: " + exc.Message);
                    }

                    curr_path_to_save = fileName;
                }
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAsHandler();
        }

        private void SaveHandler()
        {
            if (curr_path_to_save == String.Empty)
            {
                SaveAsHandler();
            }
            else
            {
                SaveFile(curr_path_to_save);
                MessageBox.Show("Файл сохранён!");
            }
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveHandler();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                SaveHandler();
                return true;
            }
            else if (keyData == (Keys.Control | Keys.O))
            {
                OpenFileHandler();
                return true;
            }
            else if (keyData == (Keys.F9))
            {
                Start();
            }
            else if (keyData == (Keys.Control | Keys.F9))
            {
                StartDebug();
            }
            else if (keyData == (Keys.F5))
            {
                if (debug_started)
                {
                    DebugStep();
                }
            }
            else if (keyData == (Keys.Control | Keys.I))
            {
                WriteIntByUser();
            }
            else if (keyData == (Keys.Control | Keys.F))
            {
                WriteFloatByUser();
            }
            else if (keyData == (Keys.Control | Keys.C))
            {
                SetRowToCodeByUser();
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void stop_button_Click(object sender, EventArgs e)
        {
            if (debug_started)
            {
                StopDebugging();
            }
            else
            {
                CodeTable.Rows[curr_pos].Cells[Command_col].Value = "ОСТ";
                CodeTable.Rows[curr_pos + 1].Cells[Command_col].Value = "ОСТ";
                CodeTable.Rows[curr_pos + 2].Cells[Command_col].Value = "ОСТ";
            }
        }

        private void ClearTabelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Очистить таблицу?", "Очистка", MessageBoxButtons.YesNo) ==
                    System.Windows.Forms.DialogResult.Yes) 
            {
                ClearTable();
            }
        }

        private void step_button_Click(object sender, EventArgs e)
        {
            DebugStep();
        }
    }

    class Row
    {
        public Row() { }
        public Row(string com, int a1, int a2, int a3)
        {
            Command = com;
            A1 = a1;
            A2 = a2;
            A3 = a3;
        }

        public String Command;
        public int A1;
        public int A2;
        public int A3;
        public int Address;
    }
}
