using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5023/")
        };

        private int? selectedItemId = null;

        public Form1()
        {
            InitializeComponent();
        }

        // Empty dummy handlers created by Designer clicks
        private void label1_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void label4_Click(object sender, EventArgs e) { }

        // 1. FORM LOAD
        private async void Form1_Load(object sender, EventArgs e)
        {
            // Force black text for all rows
            dgvItems.DefaultCellStyle.ForeColor = Color.Black;
            dgvItems.DefaultCellStyle.BackColor = Color.White;

            // Force black text for alternating rows
            dgvItems.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;
            dgvItems.AlternatingRowsDefaultCellStyle.BackColor = Color.White;

            // Keep blue highlight when a row is selected
            dgvItems.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvItems.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);

            ResetSelectionState();
            await FetchAndBindGridAsync();
            ResetSelectionState();
            await FetchAndBindGridAsync();
        }

        // 2. FETCH ALL ITEMS (GET)
        private async Task FetchAndBindGridAsync()
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync("api/inventory");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var items = JsonConvert.DeserializeObject<List<InventoryItem>>(json);

                    dgvItems.DataSource = items;
                    ResetSelectionState();
                }
                else
                {
                    MessageBox.Show($"Failed to load data: {response.StatusCode}", "API Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection error: {ex.Message}\nMake sure your Web API is running on port 5023!", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 3. INPUT VALIDATION
        private bool ValidateInputs(out decimal price)
        {
            price = 0;

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter an Item Name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtCategory.Text))
            {
                MessageBox.Show("Please enter a Category.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCategory.Focus();
                return false;
            }

            if (numQuantity.Value <= 0)
            {
                MessageBox.Show("Quantity must be greater than 0.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numQuantity.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPrice.Text) || !decimal.TryParse(txtPrice.Text.Trim(), out price) || price <= 0)
            {
                MessageBox.Show("Please enter a valid Price greater than 0.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrice.Focus();
                return false;
            }

            return true;
        }

        // 4. ADD ITEM (POST)
        private async void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs(out decimal price)) return;

            var newItem = new InventoryItem
            {
                Name = txtName.Text.Trim(),
                Category = txtCategory.Text.Trim(),
                Quantity = (int)numQuantity.Value,
                Price = price
            };

            string json = JsonConvert.SerializeObject(newItem);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync("api/inventory", content);

                if (response.IsSuccessStatusCode)
                {
                    await FetchAndBindGridAsync();
                }
                else
                {
                    MessageBox.Show($"Failed to add item: {response.StatusCode}", "API Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"API Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 5. UPDATE ITEM (PUT)
        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedItemId == null) return;
            if (!ValidateInputs(out decimal price)) return;

            var updatedItem = new InventoryItem
            {
                Id = selectedItemId.Value,
                Name = txtName.Text.Trim(),
                Category = txtCategory.Text.Trim(),
                Quantity = (int)numQuantity.Value,
                Price = price
            };

            string json = JsonConvert.SerializeObject(updatedItem);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PutAsync($"api/inventory/{selectedItemId.Value}", content);

                if (response.IsSuccessStatusCode)
                {
                    await FetchAndBindGridAsync();
                }
                else
                {
                    MessageBox.Show($"Failed to update item: {response.StatusCode}", "API Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"API Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 6. DELETE ITEM (DELETE)
        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedItemId == null) return;

            var confirm = MessageBox.Show("Are you sure you want to delete this item?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    HttpResponseMessage response = await client.DeleteAsync($"api/inventory/{selectedItemId.Value}");

                    if (response.IsSuccessStatusCode)
                    {
                        await FetchAndBindGridAsync();
                    }
                    else
                    {
                        MessageBox.Show($"Failed to delete item: {response.StatusCode}", "API Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"API Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // 7. GRID ROW SELECTION CHANGED
        private void dgvItems_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvItems.CurrentRow?.DataBoundItem is InventoryItem selectedItem)
            {
                selectedItemId = selectedItem.Id;

                txtName.Text = selectedItem.Name;
                txtCategory.Text = selectedItem.Category;
                numQuantity.Value = selectedItem.Quantity;
                txtPrice.Text = selectedItem.Price.ToString("F2");

                btnAdd.Visible = false;
                btnUpdate.Visible = true;
                btnDelete.Visible = true;
            }
        }

        // RESET UI STATE
        private void ResetSelectionState()
        {
            selectedItemId = null;
            dgvItems.ClearSelection();

            txtName.Clear();
            txtCategory.Clear();
            numQuantity.Value = 0;
            txtPrice.Clear();

            btnAdd.Visible = true;
            btnUpdate.Visible = false;
            btnDelete.Visible = false;
        }
    }
}