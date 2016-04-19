using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Loki.Bot;

namespace CommunityLib
{
    class Tasks
    {
        private static readonly TaskManager GrindBotTaskManager = Communication.GetCurrentBotTaskManager();

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

            CommunityLib.Log.ErrorFormat("[Task] Fail to add \"{0}\".", task.Name);
            if (stoponerror)
                BotManager.Stop();
            return false;
        }

        public static bool AddTask(ITask task, string name, AddType type, bool stoponerror = true)
        {
            bool added = false;
            switch (type)
            {
                case AddType.Front:
                    added = GrindBotTaskManager.AddAtFront(task);
                    break;
                case AddType.Before:
                    added = GrindBotTaskManager.AddBefore(task, name);
                    break;
                case AddType.After:
                    added = GrindBotTaskManager.AddAfter(task, name);
                    break;
                case AddType.Replace:
                    added = GrindBotTaskManager.Replace(name, task);
                    break;
            }
            if (!added)
            {
                CommunityLib.Log.ErrorFormat("[TaskHelpers] Fail to add \"{0}\".", task.Name);
                if (stoponerror)
                    BotManager.Stop();

                return false;
            }

            return true;
        }

        public static void RemoveTask(string name, bool stoponerror = true)
        {
            if (!GrindBotTaskManager.Remove(name))
            {
                CommunityLib.Log.ErrorFormat("[TaskHelpers] Fail to remove \"{0}\".", name);
                if (stoponerror) BotManager.Stop();
            }
        }
    }
}
