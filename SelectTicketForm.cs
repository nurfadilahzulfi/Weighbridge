using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Weighbridge
{
    public partial class SelectTicketForm : Form
    {
        // This will hold the selected ticket data to pass back to the main form
        public DataRow SelectedTicket { get; private set; }

        public SelectTicketForm(DataTable ticketData)
        {
            InitializeComponent();
            LoadTicketData(ticketData);
        }

        private void LoadTicketData(DataTable ticketData)
        {
            dgvTicketList.DataSource = ticketData;

            dgvTicketList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTicketList.MultiSelect = false;
            dgvTicketList.ReadOnly = true;
            dgvTicketList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvTicketList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvTicketList.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvTicketList.AllowUserToAddRows = false;
        }


        private void SelectTicketForm_Load(object sender, EventArgs e)
        {
            this.ControlBox = false;
        }

        private void dgvTicketList_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                SelectRowAndClose(e.RowIndex);
            }
        }

        private void SelectRowAndClose(int rowIndex)
        {
            // Get the underlying DataRow from the bound DataTable
            DataRowView drv = dgvTicketList.Rows[rowIndex].DataBoundItem as DataRowView;
            if (drv != null)
            {
                SelectedTicket = drv.Row;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}
