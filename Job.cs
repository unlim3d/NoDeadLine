using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ICSharpCode.SharpZipLib.BZip2;
using System.Net;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
public class Job
{
	public string Id;
	public string RenderPath;
	public string[] ExistingFiles;
	public int[] Frames;
	public bool Tested;
	public string FileOutputMask;
	public string Renderer;
	public static List<Job> Jobs = new List<Job>();
	public  List<Task> CheckedTasks;
	public  int LastMovFramesCounter=0;
	public  int MinimumFrameRendered=999999999;
	public  int MaximumFrameRendered=-1111111;
	public string CollectPath = "";
	public static void CheckJobName(List<Task> tasks)
	{
		List<string> JobPaths = new List<string>();
		for (int i = 0; i < Jobs.Count; i++)
		{
			JobPaths.Add(Jobs[i].RenderPath);
		}
		for (int i = 0; i < tasks.Count; i++)
		{
			if (JobPaths.Contains(tasks[i].OriginalPath))
			{

			}
			else
			{
				Job Joba=new Job();
				Joba.RenderPath=tasks[i].OriginalPath;
				Job.JobMaxMinComparer(Joba, tasks[i].Frame);
				JobPaths.Add(Joba.RenderPath);
				Jobs.Add(Joba);
				Joba.Renderer = tasks[i].Renderer;
				Joba.FileOutputMask = tasks[i].OriginalFileName.Substring(0,tasks[i].OriginalFileName.Length-8);
				Joba.ExistingFiles = Program.SearchFile(Joba.RenderPath, "*"+ Joba.FileOutputMask + "*");
				Joba.Id = tasks[i].JobId;
				TryParseOtherFrames();
			}
			//if (CheckedTasks == null) CheckedTasks = new List<Task>();
		//	CheckedTasks.Add(Program.tasks[i]);
		//	Program.tasks.RemoveAt(i);
		
		}
	
		TryParseOtherFrames();
	}


	public static void TryParseOtherFrames()
	{ 
		for (int i = 0; i <Jobs.Count; i++)
		{
			Console.WriteLine("\n Проверяем джобу номер: " + i.ToString() + " файлов в папке:  " + Jobs[i].ExistingFiles.Length+ ":   ");
			for (int j = 0; j < Jobs[i].ExistingFiles.Length; j++)
			{
				
				string sourcefile = Jobs[i].ExistingFiles[j];
				string output = Task.GetFileNameFromPath(sourcefile);
				output = Task.GetServerPreviewFileNameByOriginalFileName(output);
				Job.JobMaxMinComparer(Jobs[i], Task.GetFrameNumberFromFileName(Jobs[i].ExistingFiles[j]));
				if (!File.Exists(output))
					{ 
		
						Console.WriteLine(j.ToString() + " Копируем файл без репорта. ");
						Program.RunProcess(0,sourcefile,output ,Jobs[i].Renderer);
				}
				else
				{
					Console.Write("-");
				}
			}
		} 
	}


	public static void CheckSequence()
	{
		//	string htmlCode;
		//	using (WebClient client = new WebClient())
		//	{
		//		htmlCode = client.DownloadString("http://nodeadline.mykeenetic.com:8082/api/jobs");
		//}


		Console.WriteLine("\nВсего JOBS: " + Jobs.Count.ToString()); for (int i = 0; i < Jobs.Count; i++)
		{
			Console.WriteLine("\nВсего JOBS: " + Jobs[i].FileOutputMask.ToString());
		}
		 
		 
		for (int i = 0; i < Jobs.Count; i++)
		{
			Console.WriteLine(Jobs[i].FileOutputMask);
			int SequenceCounter=0;
			if (i == 4)
			{

			}
			string Frame="" ;
			for (int j = Jobs[i].MinimumFrameRendered; j <= Jobs[i].MaximumFrameRendered; j++)
			{
				Frame = j.ToString();
				if (Frame.Length == 1) Frame = "000" + Frame;
				if (Frame.Length == 2) Frame = "00" + Frame;
				if (Frame.Length == 3) Frame = "0" + Frame;
				Frame = Task.GetServerPreviewFileNameByOriginalFileName(Jobs[i].FileOutputMask + Frame+".jpg");
				if (File.Exists(Frame)) SequenceCounter++;
				else
				{
				
					if (SequenceCounter > 0) {
						if (Jobs[i].LastMovFramesCounter != j )
						{

							GenerateMovFile(Jobs[i],Frame, Jobs[i].MinimumFrameRendered, j );
							Jobs[i].LastMovFramesCounter = j;
						}
						else
						{
							Console.WriteLine("\nПропускаем MOV, так как число файлов не изменилось\n");
						}
											}
					
					break;
				}
				if(j== Jobs[i].MaximumFrameRendered)
					 GenerateMovFile(Jobs[i],Frame, Jobs[i].MinimumFrameRendered, j);

			}


		}
	}

	public static void JobMaxMinComparer(Job job,int framenumber)
	{
		if (job.MaximumFrameRendered < framenumber) job.MaximumFrameRendered = framenumber;
		if (job.MinimumFrameRendered > framenumber) job.MinimumFrameRendered= framenumber;
	}

	public static string GenerateMovFile(Job job,string path,int startFrame,int CountOfFrames)
	{
		string filemask = Task.GetFileNameFromPath(path);
		string output = Task.GetServerPreviewFileNameByOriginalFileName(filemask);
		output = output.Substring(0, output.Length - 8)+".mov -y";
		path = path.Substring(0, output.Length -7) + "%04d.jpg";
		Console.WriteLine("\nЕбушки воробушки, охуенный мов рендерим: " + startFrame.ToString() + "-" + CountOfFrames.ToString());
		
		Program.RunProcess(startFrame,path,output,null);

		output = JsonConvert.SerializeObject(job);
		File.WriteAllText(job.FileOutputMask + ".json", output);
		Console.ForegroundColor = ConsoleColor.DarkGreen;
		Console.WriteLine("\nЗаписываем JsonJob: " + job.FileOutputMask);
		Console.ForegroundColor = ConsoleColor.White;


		return null;
	}
	public static string DeleteAllSybmols(string str)
	{
		string temps="";
		for (int i = 0; i < str.Length; i++)
		{
			if ((str.Substring(i, 1) == "0")) temps += str[i];
			if ((str.Substring(i, 1) == "1")) temps += str[i];
			if ((str.Substring(i, 1) == "2")) temps += str[i];
			if ((str.Substring(i, 1) == "3")) temps += str[i];
			if ((str.Substring(i, 1) == "4")) temps += str[i];
			if ((str.Substring(i, 1) == "5")) temps += str[i];
			if ((str.Substring(i, 1) == "6")) temps += str[i];
			if ((str.Substring(i, 1) == "7")) temps += str[i];
			if ((str.Substring(i, 1) == "8")) temps += str[i];
			if ((str.Substring(i, 1) == "9")) temps += str[i];
		}
 
		
		return temps;
	}

}
