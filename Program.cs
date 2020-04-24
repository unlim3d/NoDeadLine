using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Timers;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.BZip2;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

class Program
{
    
    public const string ReportPath =@"c:\DeadlineRepository10\reports\slaves\";
    public static string FFMPEG = @"c:\DeadlineRepository10\Node\FFMPEG\ffmpeg.exe ";
    public static string NodeWWW = @"c:\DeadlineRepository10\Node\Site\";
    public static List<Task> tasks;
    public static float Counts = 0;
    public static int ChangedCountFiles = 0;
   static int iteration = 10;
         static void Main(string[] args)
    {
       // DeletePreviousRender();
         tasks = new List<Task>();
        while (true)
        {
          
            CheckNewTaskCompleted();
            if (ChangedCountFiles > 0)
            {
                Job.CheckJobName(tasks);
                Job.CheckSequence();
            }
            Console.WriteLine("\nСледующая проверка через: \n");
            for (int i = 0; i < iteration; i++)
            {
                Thread.Sleep(1000);

                Console.Write((iteration - i).ToString() + " ");
            }
            if (iteration < 300) iteration++;
        }  
    }

  static void DeletePreviousRender()
    {
        string[] strs = SearchFile(NodeWWW, "");
        for (int i = 0; i < strs.Length; i++)
        {
            File.Delete(strs[i]);
        }
        Console.WriteLine("Удалено: " + (strs.Length).ToString()+ " Файлов" ); ;
    }
   
   public static string FindToStrEnd(string str,int shift)
    {
 
        int t = str.IndexOf('\n');
        if (t != -1) return str.Substring(0, t+shift);

        else
            return str;
    }
    static void CheckNewTaskCompleted()
    {




        string[] strs = SearchFile(ReportPath, "*.bz2");
        int C = strs.Length;
        int CountOldTasks = 0;
    
        Console.WriteLine("\n "+"найдено " + C + " файлов bz2"  );
        for (int i = 0; i < strs.Length ; i++)
        {   
            bool CheckNew = true;
            for (int j = 0; j < tasks.Count; j++)
            {
                if (tasks[j].ReportPath == strs[i])
                {
                    strs[i] = "";
                    CountOldTasks++;
                    Console.Write("X");
                    CheckNew = false;
                    break;
                }
               
            }
            if(CheckNew==true) LoadBZ2(strs[i]);
        }
        Console.WriteLine( "\n "+"Старых тасков:  " + CountOldTasks.ToString()  );
        //выводим сообщение о том сколько файлов нашли

        for (int i = 0; i < tasks.Count; i++)
        {// -filter_complex=/"scale=320:-1/" " +
          //  Console.WriteLine(i.ToString()+" из "+ tasks.Count);
            DateTime modificationSource = File.GetLastWriteTime(tasks[i].OriginalPath + tasks[i].OriginalFileName);
            DateTime modificationPreview = File.GetLastWriteTime(tasks[i].ServerPreviewFileName);
            if (modificationPreview> modificationSource)
            {
                Console.Write("x");
            }
            else
            {
                RunProcess(0,tasks[i].OriginalPath + tasks[i].OriginalFileName, tasks[i].ServerPreviewFileName, tasks[i].Renderer);
                string output = JsonConvert.SerializeObject(tasks[i]);
                File.WriteAllText(tasks[i].ServerPreviewJson, output);
           
            }

          
        }
      

    
    }
    public static string[] SearchFile(string patch, string pattern)
    {
        /*флаг SearchOption.AllDirectories означает искать во всех вложенных папках*/
        string[] ResultSearch = Directory.GetFiles(patch, pattern, SearchOption.AllDirectories);
        //возвращаем список найденных файлов соответствующих условию поиска 
        return ResultSearch;
    }
    static void LoadBZ2(string path)
    {
        string zipFileName = @path;

        using (FileStream fileToDecompressAsStream = File.OpenRead(zipFileName))
        {
            MemoryStream MS = new MemoryStream();

            BZip2.Decompress(fileToDecompressAsStream, MS, false);

            MS.Seek(0, SeekOrigin.Begin);
            Task temptask = new Task();
            temptask.ReportPath = path;
            temptask.Weight = MS.Length;
            {// поиск номера кадра
                string result = Encoding.ASCII.GetString(MS.ToArray());
                MS.Close();
                fileToDecompressAsStream.Close();
                int SeekPoint = result.IndexOf("Render frame ");
                if (SeekPoint != -1)
                {

                    string temp = result.Substring(SeekPoint + 13, 6);
                    int.TryParse(FindToStrEnd(temp, 0), out temptask.Frame);
                    
                }
                else return;
                string str = @"jobsData\";
                SeekPoint = result.IndexOf(str);
                if (SeekPoint != -1)
                {
                    temptask.JobId = result.Substring(SeekPoint+str.Length, 24);
                }
                  str = "Job Submit Date:";
                str = FindToStrEnd(result.Substring(result.IndexOf(str) + 17, 19), 0);
                DateTime.TryParse(str, out temptask.CreationDate);
                str = str.Replace(@"/", "_");
                str = str.Replace(@"\", "_");
                str = str.Replace(@":", "_");
                str = str.Replace(@":", "_");
                str = str.Replace(@" ", "_");
                str = str.Replace(@"  ", "_");
                //поиск пути ргб
                temptask.SceneName = str + "____";
                SeekPoint = result.IndexOf("Slave Name: ");

                if (SeekPoint != -1)
                {
                    temptask.Slave = FindToStrEnd(result.Substring(SeekPoint + 12, 100), -1);                    
                }
                SeekPoint = result.IndexOf("Saved image to ");
                if (SeekPoint != -1)
                {
                    temptask.OriginalFileName = FindToStrEnd(result.Substring(SeekPoint + 15, 400), -1);
                }
                else
                {
                    SeekPoint = result.IndexOf("copying to ");
                    if (SeekPoint != -1)
                    {
                        temptask.OriginalFileName = FindToStrEnd(result.Substring(SeekPoint + 11, 400), -1);
                        temptask.ReportPath = path;

                    }
                    else
                    {
                        //Console.WriteLine("Не найдено Сохранения файла для:" + temptask.OriginalFileName.ToString());
                        return;
                    }
                }
                if ((result.IndexOf("V-Ray")) != -1) temptask.Renderer = "V-Ray";
                else temptask.Renderer = "RedShift";



                tasks.Add(temptask);

            }

        }
    }
  

    public static string RunProcess(int startFrame, string SourcePath, string OutPutPath, string Renderer)
    {
        string GammaCorretion;
         if(Renderer == "V-Ray")
            GammaCorretion = "   -gamma 2.2 ";
        else GammaCorretion = "   -gamma 1.0 ";
        ChangedCountFiles++;
        string offset="";
        if (startFrame != 0) offset = " -start_number " + startFrame.ToString();
       

            // -vf scale = 320:-1 "- vf scale = 320:-1, "+ + GammaCorretion
            string tmp = offset +" -i  " + SourcePath + " -s 640:360 " + "-y " + OutPutPath;
        Console.WriteLine("\nFFMPEG:  " + tmp+ "\n");
        //create a process info object so we can run our app
        ProcessStartInfo oInfo = new ProcessStartInfo (FFMPEG, tmp);
        oInfo.UseShellExecute = false;
        oInfo.CreateNoWindow = true;

        //so we are going to redirect the output and error so that we can parse the return
        oInfo.RedirectStandardOutput = true;
        oInfo.RedirectStandardError = true;

        //Create the output and streamreader to get the output
        string output = null; StreamReader srOutput = null;

        //try the process
        try
        {
            //run the process
            Process proc = System.Diagnostics.Process.Start(oInfo);

            //proc.WaitForExit();

            //get the output
            srOutput = proc.StandardError;

            //now put it in a string
            output = srOutput.ReadToEnd();

            proc.Close();
        }
        catch (Exception)
        {
            output = string.Empty;
            Console.WriteLine("Косяк бляяя:  " );
        }
        finally
        {
            //now, if we succeded, close out the streamreader
            if (srOutput != null)
            {
                srOutput.Close();
                srOutput.Dispose();
            }
        }
        return output;
    }
   

}
