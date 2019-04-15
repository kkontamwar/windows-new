using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using Utility;

namespace MigrationTool
{
    public partial class Form1 : Form
    {
        private TreeNode _root = new TreeNode("Solution");
        private TreeNode _filenode = new TreeNode("Files");
        string _path = "";
        private static Solution _solutionFile = null;
        private DirectoryInfo _parentDirectory = null;
        //private List<string> ext = new List<string> { ".cs", ".ts", ".config", " *.jpg", "*.gif", "*.cpp", "*.c", "*.htm", "*.html", "*.xsp", "*.asp", "*.xml", "*.h", "*.asmx", "*.asp", "*.atp", "*.bmp", "*.dib", "*.config", "*.sln", "*.txt" };
        static DataTable _projects = null;
        readonly char[] splitchar = new char[] { ' ', ',' };
        readonly string _filepath = Path.GetDirectoryName(Application.ExecutablePath) + "\\Autocomplete.txt";
        AutoCompleteStringCollection _autocompleteList = new AutoCompleteStringCollection();
        private GroupByGrid groupByGrid = null;
        private Dictionary<string, int> keywordsearch = new Dictionary<string, int>();

        public Form1()
        {
            InitializeComponent();
            groupByGrid = new GroupByGrid();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            listView1.LabelEdit = true;
            listView1.FullRowSelect = true;
            listView1.Sorting = SortOrder.Ascending;
            ApplyAutoFilter();
        }

        private void ApplyAutoFilter()
        {
            if (!File.Exists(_filepath))
            {
                File.Create(_filepath);
            }
            else
            {
                var list = new AutoCompleteStringCollection { "Home", "Index" };
                using (var reader = new StreamReader(_filepath))
                {
                    while (!reader.EndOfStream)
                        _autocompleteList.Add(reader.ReadLine());

                }

                txtSearchBox.AutoCompleteMode = AutoCompleteMode.Suggest;
                txtSearchBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtSearchBox.AutoCompleteCustomSource = list;
            }
        }


        private void GetProjects()
        {
            openFileDialog1.Filter = Constants.CONSTFILTER;
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.

            if (result == DialogResult.OK) // Test result.
            {
                string file = openFileDialog1.FileName;
                textBox1.Text = file;
                try
                {
                    _parentDirectory = Directory.GetParent(file);
                    _solutionFile = new Solution(file);
                    if (treeView1.Nodes.Count <= 0) treeView1.Nodes.Add(_root);

                    if (_solutionFile.Projects == null) return;
                    var solutionDir = Path.GetDirectoryName(file);
                    foreach (var proj in _solutionFile.Projects)
                    {
                        var projNode = new TreeNode(proj.ProjectName);
                        _root.Nodes.Add(projNode);

                        var prjFiles = Directory.GetFiles(solutionDir ?? throw new InvalidOperationException(), proj.ProjectName + ".*", SearchOption.AllDirectories)
                            .Where(s => s.EndsWith(".csproj"));
                        foreach (var prjFile in prjFiles)
                        {

                            try
                            {
                                XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";
                                XDocument projDefinition = XDocument.Load(prjFile);
                                IEnumerable<string> assemblyNames = projDefinition
                                    .Element(msbuild + "Project")
                                    .Elements(msbuild + "PropertyGroup")
                                    .Elements(msbuild + "AssemblyName")
                                    .Select(asmblem => asmblem.Value);
                                foreach (string AssemblyName in assemblyNames)
                                {
                                    string findedAssembly = Directory.GetFiles(solutionDir, AssemblyName + ".dll", SearchOption.AllDirectories).FirstOrDefault();

                                    Assembly a = Assembly.LoadFrom(findedAssembly);
                                    var refAssemblies = a.GetReferencedAssemblies();
                                    Module[] m = a.GetModules();
                                    Type[] projClasses = m[0].GetTypes();

                                    if (projClasses != null)
                                    {
                                        var classNode = new TreeNode("Classes");
                                        projNode.Nodes.Add(classNode);

                                        foreach (var type in projClasses)
                                        {
                                            if (type.Name.StartsWith("<>"))
                                                continue;

                                            var typeNode = new TreeNode(type.Name);
                                            classNode.Nodes.Add(typeNode);

                                            GetMethods(type, typeNode);

                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {

                                continue;
                            }
                        }
                    }
                }
                catch (IOException ex)
                {
                }
            }
        }

        private static void GetMethods(Type type, TreeNode typeNode)
        {
            MethodInfo[] mInfo = type.GetMethods();
            if (mInfo != null)
            {
                var methodNode = new TreeNode("Methods");
                typeNode.Nodes.Add(methodNode);
                foreach (var mi in from MethodInfo mi in mInfo
                                   where mi.DeclaringType == type
                                   select mi)
                {
                    methodNode.Nodes.Add(mi.Name);
                }
            }
        }

        //Data to be dispaly on the left panel
        //private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        //{
        //    for (int i = 0; i < listView1.Items.Count; i++)
        //    {
        //        if (listView1.Items[i].Selected == true)
        //        {
        //            _path = listView1.Items[i].Name;
        //            textBox1.Text = _path;
        //            listView1.Items.Clear();
        //            //LoadFilesAndDir(_path);
        //        }
        //    }
        //}

        /// <summary>
        /// Browse the solution file to explore its content.
        /// </summary>
        /// <param name="sender">button object</param>
        /// <param name="e">button event argument</param>
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            GetProjects();
        }

        /// <summary>
        /// Create default datatable header content.
        /// </summary>
        private static void CreateTemplateTable()
        {
            _projects = new DataTable();
            // add column to datatable  
            _projects.Columns.Add("Project Name", typeof(string));
            _projects.Columns.Add("File Name", typeof(string));
            _projects.Columns.Add("Search Keaywords", typeof(string));
            _projects.Columns.Add("Line No.", typeof(int));
            _projects.Columns.Add("Line", typeof(string));
        }

        private static void GetProjectOutPutType()
        {
            //DTE DTE = Package.GetGlobalService(typeof(EnvDTE.DTE)) as DTE;
            //var project = ((Array) DTE.ActiveSolutionProjects).GetValue(0) as Project;
            //var properties = project.Properties;
            //var ot = properties.Item("OutputType").Value.ToString();s
            //prjOutputType po = (prjOutputType) Enum.Parse(typeof(prjOutputType), ot);
        }

        private void treeView1_AfterSelect_1(object sender, TreeViewEventArgs e)
        {
            try
            {
                listView1.Items.Clear();
                var selectednode = e.Node;
                treeView1.SelectedNode.ImageIndex = e.Node.ImageIndex;
                selectednode.Expand();
                //txtSearchBox.Text = selectednode.FullPath;

                if (selectednode.Nodes.Count > 0)
                {
                    foreach (TreeNode n in selectednode.Nodes)
                    {

                        var lst = new ListViewItem(n.Text, n.ImageIndex);
                        lst.Name = n.FullPath.Substring(13);
                        listView1.Items.Add(lst);
                    }
                }
                else
                {
                    //MessageBox.Show(typeof( selectednode.Parent.Parent.Text));
                    listView1.Items.Add(selectednode.FullPath, selectednode.Text, selectednode.ImageIndex);
                }
            }
            catch (Exception e1)
            {
                CommonUtil.logger.Error(e1, "Something bad happened");
            }

        }

        private static void ShowMethods(Type type)
        {
            foreach (var method in type.GetMethods())
            {
                var parameters = method.GetParameters();
                var parameterDescriptions = string.Join
                (", ", method.GetParameters()
                    .Select(x => x.ParameterType + " " + x.Name)
                    .ToArray());

                Console.WriteLine("{0} {1} ({2})",
                    method.ReturnType,
                    method.Name,
                    parameterDescriptions);
            }
        }

        private void SearchText(string projectName, string filePath, string[] searchTextArray)
        {
            var counter = 0;
            string line;



            // Read the file and display it line by line.
            var file = new StreamReader(filePath);
            while ((line = file.ReadLine()) != null)
            {
                int wordfind = 0;
                //if (line.Contains(textToBeSearch))
                foreach (var textToBeSearch in searchTextArray)
                {

                    if (Regex.IsMatch(line, @"\b" + textToBeSearch + @"\b"))
                    {
                        if (!keywordsearch.Keys.Contains(Path.GetFileName(filePath).ToString()))
                            keywordsearch.Add(Path.GetFileName(filePath).ToString(), 0);

                        _projects.Rows.Add(projectName, Path.GetFileName(filePath), textToBeSearch, counter, line.Trim());
                        wordfind++;
                        keywordsearch[Path.GetFileName(Path.GetFileName(filePath).ToString())] = wordfind;
                    }

                }
                counter++;
            }
            file.Close();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            EnableDisableButtons(false);
            // string searchText = txtSearchBox.Text;

            if (string.IsNullOrEmpty(txtSearchBox.Text))
            {
                MessageBox.Show(Constants.ERRKEYWORDREQUIRED);
            }
            else
            {
                var searchTextArray = txtSearchBox.Text.Split(splitchar);
                SerializeDeSerializeKeywords(searchTextArray);

                ApplyAutoFilter();

                if (_solutionFile.Projects.Count > 0)
                {
                    CreateTemplateTable();
                    var dsDataset = new DataSet();
                    foreach (var projDetails in _solutionFile.Projects)
                    {
                        var projDirectory = Path.Combine(_parentDirectory.FullName, projDetails.RelativePath);
                        var directoryInfo = Directory.GetParent(projDirectory);

                        getFilesAndDir(projDetails.ProjectName, directoryInfo, searchTextArray);
                    }

                    if (_projects == null || _projects.Rows.Count <= 0)
                    {
                        EnableDisableButtons(true);
                        return;
                    }

                    dsDataset.Tables.Add(_projects);
                    groupByGrid1.DataSource = dsDataset.Tables[0];
                }
                else
                    MessageBox.Show(@"There are no projects in the solution !");
            }
            EnableDisableButtons(true);
        }

        private void SerializeDeSerializeKeywords(string[] searchTextArray)
        {
            var keywords = searchTextArray.ToList().Distinct();

            foreach (string key in keywords)
            {
                if (!File.ReadLines(_filepath).Any(l => l.Contains(key)))
                    File.AppendAllLines(_filepath, new List<string>() { key });
            }
        }

        private void EnableDisableButtons(bool isEnable)
        {

            btnSearch.Enabled = isEnable;
            btnBrowse.Enabled = isEnable;
        }


        private void getFilesAndDir(string projectName, DirectoryInfo dirname, string[] searchTextArray)
        {
            try
            {
                var filesInProject = Directory.GetFiles(dirname.FullName, "*.*", SearchOption.AllDirectories)
                    .Where(s => Constants.CONSTEXT.Contains(Path.GetExtension(s)));

                foreach (var file in filesInProject)
                {
                    SearchText(projectName, file, searchTextArray);
                }
            }
            catch (Exception e1)
            {
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            txtSearchBox.Text = string.Empty;
            textBox1.Text = string.Empty;
            treeView1.Nodes.Clear();

            groupByGrid1.DataSource = null;
            groupByGrid1 = null;

        }

        private void Button1_Click(object sender, EventArgs e)
        {
            string strMesage = string.Empty;
            foreach (var keyFile in keywordsearch.Keys)
            {
                strMesage += $"File = {keyFile} Keywords Found = {keywordsearch[keyFile].ToString()}" + System.Environment.NewLine;
            }

            MessageBox.Show(strMesage);
        }
    }
}
