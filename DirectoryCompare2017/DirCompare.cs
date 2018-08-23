using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace DirectoryCompare2017
{

    public partial class DirectoryComp : Form
    {
        String dirPrimary;        //Primary directory
        String dirCompare;        //Comparison directory
        Boolean itemsFound;       //Tracks if discrepances were found for output window.
        int fileCount = 0;        //Number of files processed.
        int dirCount = 0;         //Number of directories processed.
        int directoryCount, compareDirCount; // Number of directories found

        public DirectoryComp()
        {
            InitializeComponent();
        }

        private void CountDirectories()
        {
            try
            {
                directoryCount = Directory.GetDirectories(dirPrimary, "*.*", SearchOption.AllDirectories).Count();
                compareDirCount = Directory.GetDirectories(dirCompare, "*.*", SearchOption.AllDirectories).Count();
                directoryCount += compareDirCount;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void compareToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Compare the two directories to each other and report on 
            // the findings.

            itemsFound = false;
            
            try
            {
                // Reset the display.
                InitializeGridColumns();
                dgvResults.Rows.Clear();
                tvFolderView.Nodes.Clear();

                if (ValidateInputs())
                {
                    // Validate the directories entered before proceeding.
                    dirPrimary = tbPrimary.Text;
                    dirCompare = tbSecondary.Text;

                    // Count all directories and subdirectories for progress bar.
                    // We're comparing both ways so it has to be the sum of both primary and secondary.
                    // This might take a few moments so do it in a new thread.
                    ProgramStatus1.Text = "Getting count of directories ... ";
                    Thread t = new Thread(CountDirectories);
                    t.Start();

                    // Initialize progress bar.
                    pbProgress.Maximum = 100000;
                    pbProgress.Value = 1;
                    pbProgress.Visible = true;

                    //Reset file and directory counts.
                    dgvResults.BackColor = Color.FromName("Control");
                    dgvResults.ForeColor = Color.Black;
                    fileCount = 0;
                    dirCount = 0;
                    ProgramStatus1.Text = "Comparing secondary directory to primary.";
                    CompareDirectories(dirPrimary);

                    //Switch the directories and compare them in the opposite direction.
                    ProgramStatus1.Text = "Comparing primary directory to secondary.";
                    dirPrimary = tbSecondary.Text;
                    dirCompare = tbPrimary.Text;
                    CompareDirectories(dirPrimary);

                    // After comparison is finished, report.
                    ProgramStatus1.Text = fileCount.ToString() + " files tested.";
                    if (!itemsFound && dgvResults.Rows.Count == 0)
                    {
                        txtReport.Text = "All files match!";
                        txtReport.BackColor = Color.Green;
                        txtReport.ForeColor = Color.White;
                        ProgramStatus2.Text = "All files and folders match.";
                    }
                    else
                    {
                        txtReport.Text = "Files / folders missing!";
                        txtReport.BackColor = Color.Red;
                        txtReport.ForeColor = Color.White;
                        ProgramStatus2.Text = "File / folder mismatch.";
                    }

                    pbProgress.Value = 0;
                    pbProgress.Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error ...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void AddDirectorytoTreeView(string DirectoryPath)
        {
            // Add a specific directory to its proper location in the 
            // TreeView
            DirectoryInfo currentDir = new DirectoryInfo(DirectoryPath);
            string ParentPath = "";

            try
            {
                // Get the parent directory if needed.
                if (DirectoryPath.Contains("\\"))
                    ParentPath = DirectoryPath.Substring(0, DirectoryPath.LastIndexOf("\\"));

                TreeNode[] dirNodeFind = tvFolderView.Nodes.Find(DirectoryPath, true);
                TreeNode[] parentNodeFind = tvFolderView.Nodes.Find(ParentPath, true);
                
                // Look for the directory and its parent to place it in the TreeView.
                // If it's already there, do nothing.
                if (dirNodeFind.Length == 0)
                {
                    if (parentNodeFind.Length > 0)
                    {
                        // If the parent has been found, insert the new directory.
                        parentNodeFind[0].Nodes.Add(DirectoryPath, currentDir.Name);
                    }
                    else
                    {
                        // Otherwise, add it as a new top level.
                        tvFolderView.Nodes.Add(DirectoryPath, currentDir.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error ...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void UpdateTreeViewStatus(string DirectoryPath, Color colorCode, string ToolTipMessage)
        {

            TreeNode[] dirNodeFind = tvFolderView.Nodes.Find(DirectoryPath, true);
            TreeNode foundNode;
            
            try
            {
                // Change the appearance of the specified tree node.
                if (dirNodeFind.Length > 0)
                {
                    foundNode = dirNodeFind[0];
                    foundNode.BackColor = colorCode;
                    foundNode.ForeColor = Color.White;
                    foundNode.ToolTipText = ToolTipMessage;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error ...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

        }

        private DataGridViewRow CompareFiles(string firstFile, string secondFile)
        {
            FileInfo firstFileInfo = new FileInfo(firstFile);
            FileInfo secondFileInfo = new FileInfo(secondFile);
            String resultMessage = "";
            DataGridViewRow resultRow = new DataGridViewRow();

            try
            {
                // Compare modification dates.

                if (firstFileInfo.LastWriteTime != secondFileInfo.LastWriteTime)
                {
                    if (secondFileInfo.LastWriteTime > firstFileInfo.LastWriteTime)
                        resultMessage = secondFile + " appears to be newer than " + firstFile + ". ";
                    else
                        resultMessage = firstFile + " appears to be newer than " + secondFile + ". ";
                }

                // Compare file sizes.

                if (firstFileInfo.Length != secondFileInfo.Length)
                {
                    if (secondFileInfo.Length > firstFileInfo.Length)
                        resultMessage += secondFile + " is larger than " + firstFile + ". ";
                    else
                        resultMessage += firstFile + " is larger than " + secondFile + ". ";
                }

                resultRow.CreateCells(dgvResults, "File", secondFile, resultMessage);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error ...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            return resultRow;

        }

        private void CompareDirectories(string DirectoryPath)
        {
            // Get the number of files and subdirectories under this path.
            int nbrFiles = Directory.GetFiles(DirectoryPath).Count();
            int nbrFolders = Directory.GetDirectories(DirectoryPath).Count();
        
            // Get the corresponding directory.
            string CorrespondingPath = dirCompare + DirectoryPath.Substring(dirPrimary.Length);
            string comparisonFile, fileNameOnly;
            DataGridViewRow fileComparisonResult;

            // Increment directory count and update progress bar for every 10.
            dirCount += 1;
            if(dirCount % 10 == 0)
            {
                //if (dirCount > pbProgress.Maximum)
                if (compareDirCount > 0)
                {
                    // Update the progress bar maximum with the number of anticipated directories if it's
                    // available. Otherwise, just set it as 10 x the number processed so far.
                    //pbProgress.Maximum = directoryCount > dirCount ? directoryCount : dirCount * 10;
                    pbProgress.Maximum = directoryCount;
                }

                pbProgress.Value = dirCount;
            }

            try
            {
                // Add the directories to the TreeView if they're not already there.
                AddDirectorytoTreeView(DirectoryPath);
                AddDirectorytoTreeView(CorrespondingPath);

                if (Directory.Exists(CorrespondingPath))
                {
                    if (nbrFiles > 0)
                    {
                        // fileName returns full path and file.
                        foreach (string primaryFile in Directory.GetFiles(DirectoryPath))
                        {
                            // Increment file count.
                            fileCount += 1;
                            //Determine filepath / name of corresponding file by replacing the path ...
                            fileNameOnly = Path.GetFileName(primaryFile);
                            comparisonFile = CorrespondingPath + "\\" + fileNameOnly;
                            if (!System.IO.File.Exists(comparisonFile))
                            {
                                dgvResults.Rows.Add(FileNotice(fileNameOnly, DirectoryPath, CorrespondingPath));
                                UpdateTreeViewStatus(CorrespondingPath, Color.Orange, "Missing files");
                                itemsFound = true;
                            }
                            else
                            {
                                // If the user has opted to check file sizes and dates ...
                                if (chkFileCompare.Checked)
                                {
                                    fileComparisonResult = CompareFiles(primaryFile, comparisonFile);
                                    // A match should not return anything for the third cell in the row.
                                    if (fileComparisonResult.Cells.Count > 0)
                                    {
                                        if(fileComparisonResult.Cells[2].Value.ToString().Length > 0)
                                        {
                                            // If there is a result message, add it to the data grid.
                                            dgvResults.Rows.Add(fileComparisonResult);
                                            UpdateTreeViewStatus(CorrespondingPath, Color.Yellow, "Mismatched files");
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (nbrFolders > 0)
                    {
                        foreach (string folderName in Directory.GetDirectories(DirectoryPath))
                        {
                            ProgramStatus2.Text = "Reading " + folderName;
                            // Recursively check subdirectories if they exist.
                            if (Directory.Exists(CorrespondingPath))
                            {
                                CompareDirectories(folderName);
                            }
                            else
                            {
                                //If the corresponding directory does not exist, add the necessary information to the results.
                                dgvResults.Rows.Add(DirectoryNotice(CorrespondingPath, folderName));
                                UpdateTreeViewStatus(folderName, Color.Red, "Missing Corresponding Folder");
                                itemsFound = true;
                            }

                            Application.DoEvents();
                        }
                    }
                }
                else
                {
                    //If the corresponding path doesn't exist, just record the one entry.
                    dgvResults.Rows.Add(DirectoryNotice(CorrespondingPath, DirectoryPath));
                    UpdateTreeViewStatus(CorrespondingPath, Color.Red, "Missing Folder");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error ...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void InitializeGridColumns()
        {
            try
            {
                if(dgvResults.Columns.Count == 0)
                {
                    dgvResults.Columns.Add("Type", "Type");
                    dgvResults.Columns.Add("Name", "Item Name");
                    dgvResults.Columns.Add("Description", "Description");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error ...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private DataGridViewRow DirectoryNotice(string altDirectory, string folderName)
        {

            DataGridViewRow returnRow = new DataGridViewRow();
            //Type, Name, Corresponding, Description
            string discrepancy = "Missing directory - this folder would correspond to the existing folder " + folderName + ".";
            returnRow.CreateCells(dgvResults, "Directory", altDirectory, discrepancy);

            return returnRow;
        }

        private DataGridViewRow FileNotice(string fileNameOnly, string directoryPath, string altDirectory)
        {
            DataGridViewRow returnRow = new DataGridViewRow();

            try
            {
                returnRow.CreateCells(dgvResults, "File", fileNameOnly, "File does not exist in: " + altDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error ...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            return returnRow;

        }

        private bool ValidateInputs()
        {
            //Validate directory inputs.
            try
            {
                bool primaryPass = false, secondaryPass = false;
                bool returnValue = false;

                //Check for missing directory entries and directories that don't exist.
                //Primary directory

                if (tbPrimary.Text.Length == 0)
                {
                    errorDisplay.SetError(tbPrimary, "Please enter a directory.");
                }
                else if (!Directory.Exists(tbPrimary.Text))
                {
                    errorDisplay.SetError(tbPrimary, "This directory does not exist. Please try again.");
                }
                else
                {
                    errorDisplay.SetError(tbPrimary, "");
                    primaryPass = true;
                }

                //Secondary directory
                if (tbSecondary.Text.Length == 0)
                {
                    errorDisplay.SetError(tbSecondary, "Please enter a directory.");
                }
                else if (!Directory.Exists(tbSecondary.Text))
                {
                    errorDisplay.SetError(tbSecondary, "This directory does not exist. Please try again.");
                }
                else
                {
                    errorDisplay.SetError(tbSecondary, "");
                }
                secondaryPass = true;

                //If both fields have passed, then return true.
                if (primaryPass && secondaryPass)
                    returnValue = true;

                return returnValue;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error while verifying selected directories.", ex);
            }
        }

        private void SaveOutput(object sender, EventArgs e)
        {
            StreamWriter resultsOutput;
            try
            {
                // Prompt user for file location to save text file with results.
                if(SaveDialog.ShowDialog() == DialogResult.OK)
                {
                    // Verify path exists.
                    if (SaveDialog.CheckPathExists)
                    {
                        resultsOutput = new StreamWriter(SaveDialog.FileName);

                        // Output all rows from the results grid to the text file.
                        foreach(DataGridViewRow dgvRow in dgvResults.Rows)
                        {
                            foreach(DataGridViewCell dgvCell in dgvRow.Cells)
                            {
                                resultsOutput.WriteLine(dgvCell.Value);
                            }

                            resultsOutput.WriteLine("");
                        }

                        // Make sure everything is written and save.
                        resultsOutput.Flush();
                        resultsOutput.Close();

                        ProgramStatus1.Text = "File saved to " + SaveDialog.FileName + ".";
                    }
                    else
                        MessageBox.Show("That location does not exist. Please select a valid directory in which to save the file.", "Error saving file ...");
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error ...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Close the form.
            this.Close();
        }

        private void clearOutputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //Clear the output field and status bar.
                dgvResults.Rows.Clear();
                tvFolderView.Nodes.Clear();
                ProgramStatus1.Text = "";
                ProgramStatus2.Text = "";
                txtReport.Text = "";
                txtReport.BackColor = Color.FromName("Menu");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error ...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void FolderSearch(object sender, EventArgs e)
        {
            //Show folder dialog for user to select directory.

            Button btnPress = (sender as Button);
            
            try
            {
                //Set description on folder dialog based on which field is being completed.
                if(btnPress.Name == "btnPrimaryDir")
                    folderDialog.Description = "Select a folder and click OK to use it as the primary directory.";
                else
                    folderDialog.Description = "Select a folder and click OK to use it as the comparison directory.";

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    if (btnPress.Name == "btnPrimaryDir")
                        tbPrimary.Text = folderDialog.SelectedPath;
                    else
                        tbSecondary.Text = folderDialog.SelectedPath;
                }               
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error ...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void tvFolderView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                // Set the tooltip for the folder browser to match the last selected node.
                TreeNode tvnCurrent = tvFolderView.SelectedNode;
                toolTip1.SetToolTip(tvFolderView, tvnCurrent.ToolTipText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error ...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

        }

        private void btnExpand_Click(object sender, EventArgs e)
        {
            try
            {
                tvFolderView.ExpandAll();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}


