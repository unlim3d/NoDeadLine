using System;
using System.Collections.Generic;

public class Task
{
    private string _OriginalFileName;
    public string OriginalFileName
    {
        set
        {
          
            
            _OriginalFileName = GetFileNameFromPath(value);
            OriginalPath = GetPathWithoutFileName(value);
            _OriginalFileName = _OriginalFileName.Replace(@"\", "");
        }
        get
        {
            return _OriginalFileName;
        }
    }

    private string GetPathWithoutFileName(string value)
    {
        int lastPathSymbol = value.LastIndexOf(@"\");
        return value.Substring(0, lastPathSymbol + 1);
    }

    public string ServerPreviewFileName
    {
        get
        {
            return GetServerPreviewFileNameByOriginalFileName(OriginalFileName);
        }
    }
   public static string GetServerPreviewFileNameByOriginalFileName(string value)
    {
        string str = (Program.NodeWWW + value);
        str = str.Substring(0, str.Length - 4);
        str = str.Replace(@"\\", @"\");
        return   str+".jpg";
    }
    public string ServerPreviewJson
    {
        get
        {
            return ServerPreviewFileName.Substring(0, ServerPreviewFileName.Length - 4)+".json";
        }
    }
    public string SceneName;
    public float Weight;
    public float RenderTime;
    public string Slave;
    public int Frame;
    public string ReportPath;
    public DateTime CreationDate;
    public string Renderer;
    public string OriginalPath;
    public string JobId;

     
    public static string GetFileNameFromPath(string path)
    {
        int lastPathSymbol = path.LastIndexOf(@"\");
        if (lastPathSymbol != -1)
        {
            string str = path.Substring(lastPathSymbol, path.Length - lastPathSymbol);
            str = str.Replace(@"\", @"");
            return str;
        }
        else
        {
            return path;
        }
    }
    public static int GetFrameNumberFromFileName(string filename)
    {

        int frame = 0;
        int.TryParse(filename.Substring(filename.Length - 8, 4), out frame);
        return frame;
    }
}
