namespace ObjectsFromXml
{
    public interface ILog
    {
        void Error(string v);
        void ErrorFormat(string v, params object[] values);
        void Warn(string v);
        void WarnFormat(string v, params object[] values);
        void Info(string v);
        void InfoFormat(string v, params object[] values);
        void Critical(string v);
        void CriticalFormat(string v, params object[] values);
    }
}