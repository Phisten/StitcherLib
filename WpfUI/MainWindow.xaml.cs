using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfUI
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary> 正常拼接  </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //選擇檔案路徑
            OpenFileDialog file = new OpenFileDialog();
            file.Multiselect = true;
            file.ShowDialog();
            String argStr = String.Join(" ", file.FileNames);

            if (file.FileNames.Length > 0)
            {
                Process StitcherLib = new Process();
                StitcherLib.StartInfo.FileName = "StitcherLib.exe";
                StitcherLib.StartInfo.Arguments = argStr + textBox_arg1.Text;


                StitcherLib.Start();
            }
        }

        /// <summary> 大量拼接 
        /// 依檔名中最後一次出現'_'字元的位置開始分割字串 後部檔名相同的檔案會歸為同一類
        /// 例如:   1_456_1157.jpg  與 abc_11_1157.jpg 與 2_1157.jpg 會被分為同一組進行拼接 (由於後部檔名皆為 _1157.jpg )
        ///     而  1_456_1158.jpg  與 2_12_1158.jpg 會分為第二組進行拼接
        ///     
        /// *只會搜索使用者所選擇的資料夾以及其第一層子資料夾
        /// *目前只搜索jpg檔,其餘檔案分類為null不處理
        /// 
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = System.Windows.Forms.Application.StartupPath;
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    List<string> fileList = new List<string>();
                    fileList.AddRange(Directory.GetFiles(fbd.SelectedPath));
                    foreach (var item in Directory.GetDirectories(fbd.SelectedPath))
                    {
                        fileList.AddRange(Directory.GetFiles(item));
                    }

                    // file group filter
                    var dataGroup = fileList.GroupBy(
                        x =>
                        {
                            var y = new FileInfo(x);
                            if (y.Extension.ToLower() == ".jpg")
                            {
                                return y.Name.Substring(y.Name.LastIndexOf('_'));
                            }
                            return null;
                        });
                    int dataGroupCount = dataGroup.Count();

                    //建構批次檔
                    String WriteTxt;
                    WriteTxt = String.Empty;
                    string batFileName = DateTime.Now.ToString("yyyyMMdd HHmmss", CultureInfo.InstalledUICulture) + ".bat";
                    using (StreamWriter sw = new StreamWriter(batFileName))
                    {
                        sw.WriteLine("@ECHO OFF");
                        int curIndex = 0;
                        foreach (var item in dataGroup)
                        {
                            curIndex++;
                            if (item.Key == null)
                            {
                                sw.WriteLine("Skip invaild Files");
                                sw.WriteLine(String.Join(",", item.ToArray()));
                                continue;
                            }

                            String argStr = String.Join(" ", item.ToArray());

                            sw.WriteLine("ECHO -----------------------------------------------------------------------------------------");
                            sw.WriteLine("ECHO Stitching: " + item.Key + " ,Process:" + Math.Round(curIndex*100.0/dataGroupCount,2) + "%% (" + curIndex + "/" + dataGroupCount + ")");
                            sw.WriteLine("StitcherLib.exe " + argStr + " --output " + item.Key + textBox_arg1.Text);
                        }
                        sw.WriteLine("ECHO All file Stitching");
                        sw.WriteLine("PAUSE");
                        sw.Close();
                    }

                    //拼接前確認
                    var userConfirm = System.Windows.Forms.MessageBox.Show("批次檔建構完成,是否直接執行?\n" + batFileName + "\n檔案數量: " + fileList.Count.ToString() + "\n檔案組數: " + dataGroup.Count(), "大量拼接", MessageBoxButtons.OKCancel);
                    if (userConfirm == System.Windows.Forms.DialogResult.OK)
                    {
                        Process StitcherLib = new Process();
                        StitcherLib.StartInfo.FileName = batFileName;
                        StitcherLib.Start();
                    }
                }
            }
        }
    }
}
