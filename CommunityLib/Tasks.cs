using System.Collections.Generic;
using System.Linq;
using Loki.Bot;

namespace CommunityLib
{
    public static class Tasks
    {
        public static TaskManager CurrentTaskManager => Communication.GetCurrentBotTaskManager();

        public enum AddType
        {
            Front,
            Before,
            After,
            Replace
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="task"></param>
        /// <param name="namesToCheck">List or array of tasks you want to try to process. Returns on first sucess</param>
        /// <param name="type"></param>
        /// <param name="stoponerror"></param>
        /// <returns></returns>
        public static bool AddTask(ITask task, IEnumerable<string> namesToCheck, AddType type, bool stoponerror = true)
        {
            if (namesToCheck.Any(name => AddTask(task, name, type, false)))
                return true;

            CommunityLib.Log.ErrorFormat($"[Task](Array) Failed to add \"{task.Name}\".");
            if (stoponerror)
                BotManager.Stop();
            return false;
        }

        public static bool AddTask(ITask task, string name, AddType type, bool stoponerror = true)
        {
            CommunityLib.Log.DebugFormat($"[Task](Single) Adding task \"{task.Name}\".");

            bool added = false;
            switch (type)
            {
                case AddType.Front:
                    added = CurrentTaskManager.AddAtFront(task);
                    break;
                case AddType.Before:
                    added = CurrentTaskManager.AddBefore(task, name);
                    break;
                case AddType.After:
                    added = CurrentTaskManager.AddAfter(task, name);
                    break;
                case AddType.Replace:
                    added = CurrentTaskManager.Replace(name, task);
                    break;
            }
            if (!added)
            {
                if (stoponerror)
                {
                    CommunityLib.Log.ErrorFormat("[TaskHelpers](Single) Failed to add \"{0}\".", task.Name);
                    BotManager.Stop();
                }

                return false;
            }

            CommunityLib.Log.DebugFormat($"[Task](Single) Task \"{task.Name}\" added succesfully.");
            return true;
        }

        public static void RemoveTask(string name, bool stoponerror = true)
        {
            if (!CurrentTaskManager.Remove(name))
            {
                if (stoponerror)
                {
                    CommunityLib.Log.ErrorFormat("[TaskHelpers] Failed to remove \"{0}\".", name);
                    BotManager.Stop();
                }
            }
        }

        public static bool Exists(string name)
        {
            var n = CurrentTaskManager.GetTaskByName(name);
            return n != null;
        }
    }
}
