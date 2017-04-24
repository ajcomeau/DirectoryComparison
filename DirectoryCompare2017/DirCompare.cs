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

namespace DirectoryCompare2017
{

    public partial class DirectoryComp : Form
    {
        String dirPrimary;        //Primary directory
        String dirCompare;        //Comparison directory
        int fileCount = 0;        //Number of files processed.
        Boolean itemsFound;       //Tracks if discrepances were found for output window.
        String vbCrLf = "\r\n";

        public DirectoryComp()
        {
            InitializeComponent();
        }

        private void compareToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbResults.Text = "";
            itemsFound = false;

            try
            {
                if (ValidateInputs())
                {
                    // Validate the directories entered before proceeding.
                    dirPrimary = tbPrimary.Text;
                    dirCompare = tbSecondary.Text;

                    //Reset filecount.
                    rtbResults.BackColor = Color.FromName("Control");
                    rtbResults.ForeColor = Color.Black;
                    fileCount = 0;
                    ProgramStatus1.Text = "Comparing secondary directory to primary.";
                    CompareDirectories(dirPrimary);
                    //Switch the directories and compare them in the opposite direction.
                    ProgramStatus1.Text = "Comparing primary directory to secondary.";
                    dirPrimary = tbSecondary.Text;
                    dirCompare = tbPrimary.Text;
                    CompareDirectories(dirPrimary);
                    // After comparison is finished, report.
                    ProgramStatus1.Text = fileCount.ToString() + " files tested.";
                    if (!itemsFound && rtbResults.Text.Length == 0)
                    {
                        rtbResults.Text += "All files match!";
                        rtbResults.BackColor = Color.Green;
                        rtbResults.ForeColor = Color.White;
                        ProgramStatus2.Text = "All files and folders match.";
                    }
                    else
                    {
                        rtbResults.Text += "Files / folders missing!";
                        rtbResults.BackColor = Color.Red;
                        rtbResults.ForeColor = Color.White;
                        ProgramStatus2.Text = "File / folder mismatch.";
                    }
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error ...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

        }


        private void CompareDirectories(string DirectoryPath)
        {
            int nbrFiles = Directory.GetFiles(DirectoryPath).Count();
            int nbrFolders = Directory.GetDirectories(DirectoryPath).Count();
            string comparisonFile;
            string fileNameOnly, altDirectory;

            try
            {
                if (nbrFiles > 0)
                {
                    foreach (string fileName in Directory.GetFiles(DirectoryPath))
                    {
                        // Increment file count.
                        fileCount += 1;
                        //Determine filepath / name of corresponding file ...
                        comparisonFile = dirCompare + fileName.Substring(dirPrimary.Length);
                        if (!System.IO.File.Exists(comparisonFile))
                        {
                            fileNameOnly = Path.GetFileName(fileName);
                            altDirectory = comparisonFile.Substring(0, comparisonFile.Length - fileNameOnly.Length);
                            rtbResults.Text += FileNotice(fileNameOnly, DirectoryPath, altDirectory);
                            itemsFound = true;
                        }
                    }
                }

                if (nbrFolders > 0)
                {
                    foreach (string folderName in Directory.GetDirectories(DirectoryPath))
                    {
                        ProgramStatus2.Text = "Reading " + folderName;
                        altDirectory = dirCompare + folderName.Substring(dirPrimary.Length);
                        // Recursively check subdirectories if they exist.
                        if (Directory.Exists(altDirectory))
                        {
                            CompareDirectories(folderName);
                        }
                        else
                        {
                            //If the corresponding directory does not exist, add the necessary information to the results.
                            rtbResults.Text += DirectoryNotice(altDirectory, folderName);
                            itemsFound = true;
                        }
                        Application.DoEvents();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error ...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }



        private string DirectoryNotice(string altDirectory, string folderName)
        {
            string returnString;

            //Assemble text for addition to output box.
            returnString = "Directory missing: " + vbCrLf;
            returnString += altDirectory + " does not exist." + vbCrLf;
            returnString += "This folder would correspond to the existing folder" + vbCrLf;
            returnString += folderName + vbCrLf + "found within the primary directory." + vbCrLf + vbCrLf;

            return returnString;
        }

        private string FileNotice(string fileNameOnly, string directoryPath, string altDirectory)
        {
            string returnString;

            //Assemble text for addition to output box.
            returnString = "File Missing: " + vbCrLf;
            returnString += fileNameOnly + vbCrLf;
            returnString += "Exists in " + directoryPath + vbCrLf;
            returnString += "Does not exist in " + altDirectory + vbCrLf + vbCrLf;

            return returnString;
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

            try
            {
                if(SaveDialog.ShowDialog() == DialogResult.OK)
                {
                    if (SaveDialog.CheckPathExists)
                    {
                        rtbResults.SaveFile(SaveDialog.FileName, RichTextBoxStreamType.RichText);
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
            //Clear the output field and status bar.
            rtbResults.Text = "";
            rtbResults.BackColor = Color.FromName("Control");
            rtbResults.ForeColor = Color.Black;
            ProgramStatus1.Text = "";
            ProgramStatus2.Text = "";
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


    }
}


