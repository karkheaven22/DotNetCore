using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;

public class FileLogger : ILogger
{
    protected static string delimiter = new string(new char[] { (char)1 });

    public FileLogger(string categoryName)
    {
        this.Name = categoryName;
    }

    private class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private readonly Disposable _DisposableInstance = new Disposable();

    public IDisposable BeginScope<TState>(TState state)
    {
        return _DisposableInstance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return this.MinLevel <= logLevel;
    }

    public void Reload()
    {
        _Expires = true;
    }

    public string Name { get; private set; }

    public LogLevel MinLevel { get; set; }
    public string FileDiretoryPath { get; set; }
    public string FileNameTemplate { get; set; }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!this.IsEnabled(logLevel))
            return;
        var msg = formatter(state, exception);
        this.Write(logLevel, eventId, msg, exception);
    }

    private void Write(LogLevel logLevel, EventId eventId, string message, Exception ex)
    {
        EnsureInitFile();

        //TODO 提高效率 队列写！！！
        var log = String.Concat(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), delimiter, '-', delimiter, logLevel.ToString(), ':',
                delimiter, message, delimiter, ex?.ToString(), delimiter, ex?.StackTrace);
        lock (this)
        {
            this._sw.WriteLine(log);
        }
    }

    private bool _Expires = true;
    private string _FileName;
    protected StreamWriter _sw;

    private void EnsureInitFile()
    {
        if (CheckNeedCreateNewFile())
        {
            lock (this)
            {
                if (CheckNeedCreateNewFile())
                {
                    InitFile();
                    _Expires = false;
                }
            }
        }
    }

    private bool CheckNeedCreateNewFile()
    {
        if (_Expires)
        {
            return true;
        }
        //TODO 使用 RollingType判断是否需要创建文件。提高效率！！！
        if (_FileName != DateTime.Now.ToString(this.FileNameTemplate))
        {
            return true;
        }
        return false;
    }

    private void InitFile()
    {
        if (!Directory.Exists(this.FileDiretoryPath))
        {
            Directory.CreateDirectory(this.FileDiretoryPath);
        }

        int i = 0;
        string path;
        do
        {
            _FileName = DateTime.Now.ToString(this.FileNameTemplate);
            path = Path.Combine(this.FileDiretoryPath, _FileName + "_" + i + ".log");
            i++;
        } while (System.IO.File.Exists(path));
        var oldsw = _sw;
        _sw = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read), Encoding.UTF8)
        {
            AutoFlush = true
        };
        if (oldsw != null)
        {
            try
            {
                _sw.Flush();
                _sw.Dispose();
            }
            catch
            {
            }
        }
    }
}