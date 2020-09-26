using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace dbProviderFactory
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        DbConnection conn = null;
        DbProviderFactory fact = null;

        private void button2_Click(object sender, EventArgs e)
        {
            DataTable t = DbProviderFactories.GetFactoryClasses();
            dataGridView1.DataSource = t;
            comboBox1.Items.Clear();
            foreach (DataRow dr in t.Rows)
            {
                comboBox1.Items.Add(dr["InvariantName"]);
            }
            comboBox1.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var adapter = fact.CreateDataAdapter();
            adapter.SelectCommand = conn.CreateCommand();
            adapter.SelectCommand.CommandText = textBox2.Text.ToString();

            var set = new DataSet();
            adapter.Fill(set);

            DataViewManager dvm = new DataViewManager(set);
            dvm.DataViewSettings[0].RowFilter = "id < 100";
            dvm.DataViewSettings[0].Sort = "Title ASC";


            var view = dvm.CreateDataView(set.Tables[0]);
            dataGridView1.DataSource = view;
        }

        private string GetConnectionStringByProvider(string providerName)
        {
            var settings = ConfigurationManager.ConnectionStrings;
            if(settings != null)
            {
                foreach(ConnectionStringSettings cs in settings)
                {
                    if (cs.ProviderName == providerName)
                        return cs.ConnectionString;
                }
            }
            return string.Empty;
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {   
             textBox1.Clear();
            textBox1.Text = GetConnectionStringByProvider(comboBox1.SelectedItem.ToString());
            fact = DbProviderFactories.GetFactory(comboBox1.SelectedItem.ToString());
            conn = fact.CreateConnection();
            conn.ConnectionString = textBox1.Text;

        }
        DataTable table = null;

        private void Callback(IAsyncResult result)
        {
            try
            {
                SqlCommand command = (SqlCommand)result.AsyncState;
                var dataReader = command.EndExecuteReader(result);
                table = new DataTable();
                do
                {
                    for (int i = 0; i < dataReader.FieldCount; i++)
                        table.Columns.Add(dataReader.GetName(i));
                    while (dataReader.Read())
                    {
                        var row = table.NewRow();
                        for (int i = 0; i < dataReader.FieldCount; i++)
                            row[i] = dataReader[i];
                        table.Rows.Add(row);
                    };

                }
                while (dataReader.NextResult());

                if(conn != null)
                {
                    conn.Close();
                }
                ShowData();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ShowData()
        {
            if (dataGridView1.InvokeRequired)
            {
                dataGridView1.Invoke(new Action(ShowData));
                return;
            }
            dataGridView1.DataSource = table;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            const string asyncEnable = "Asynchronous Processing=true";
            if (!textBox1.Text.Contains(asyncEnable))
            {
                textBox1.Text = string.Format("{0};{1}", textBox1.Text, asyncEnable);
            }
            conn.ConnectionString = textBox1.Text;
            conn.Open();

            using( var comm = (conn as SqlConnection).CreateCommand())
            {
                comm.CommandText = $"WAITFOR DELAY '00:00:05'; select * from books;";
                comm.CommandType = CommandType.Text;
                comm.BeginExecuteReader(Callback, comm);
                MessageBox.Show("Added thread is working...");
            }
        }
    }
}
