using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Taoyl.Application.MesDriver.Interfaces;
using Taoyl.Application.MesDriver.Models;

namespace Taoyl.Application.MesDriver.Config
{
    /// <summary>
    /// 配置快照管理。文件读写在 MesConfigStore，这里只负责「换快照」这件事。
    ///
    /// 🔴 并发契约（整个驱动线程安全的地基）：
    /// 1. 快照整体替换——Volatile.Write 换引用，绝不原地增删 Current 里的集合；
    /// 2. 快照发布后冻结——Save 构造全新的 MesConfig，未改动条目复用引用，被改的条目必是 Clone；
    /// 3. 写操作串行——Save / Delete / Reload 共用 writeLock；
    /// 4. 先落盘后换快照——落盘失败不换，磁盘与内存不允许不一致；
    /// 5. 唯一键——接口名是 FindApi / Delete / 左栏定位的定位键，Save 对「新增」与「改名」
    ///    都查重，重复名一律拒写（否则改的那条永不生效、删一次删两条）。
    /// </summary>
    public class MesConfigManager : IMesConfigManager
    {
        private readonly MesConfigStore store;
        private readonly object writeLock = new object();
        private MesConfig current;
        private string lastSaveError;

        public MesConfigManager(string filePath)
        {
            store = new MesConfigStore(filePath);
            current = store.Load();
        }

        /// <summary>配置文件路径，供调试界面显示。</summary>
        public string FilePath => store.FilePath;

        public string LastLoadError => store.LastLoadError;

        /// <summary>
        /// 最近一次保存 / 删除的失败描述。
        /// 🔴 非 IO 失败（查重、条目不存在）由本类设文案；IO 失败本类不设，透出 Store 的落盘错误。
        /// 不透传的话，UI 的「保存失败：xxx」在查重场景只能显示「未知原因」——
        /// 因为 MesConfigStore.Save 根本没被调用，它的 LastSaveError 是 null。
        /// </summary>
        public string LastSaveError => lastSaveError ?? store.LastSaveError;

        public MesConfig Current => Volatile.Read(ref current);

        public void Reload()
        {
            lock (writeLock)
            {
                Volatile.Write(ref current, store.Load());
            }
        }

        public MesApiDefinition GetApi(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return Find(Current, name)?.Clone();
        }

        public IReadOnlyList<string> GetApiNames()
        {
            return Current.Apis.Select(api => api.Name).ToList();
        }

        public bool Save(string originalName, MesApiDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Name))
                return false;

            lock (writeLock)
            {
                lastSaveError = null;

                MesConfig snapshot = Current;
                List<MesApiDefinition> apis = snapshot.Apis.ToList();
                int index = IndexOfName(apis, originalName);   // -1 = 新增；有值但条目已被删也落入追加

                // 🔴 唯一键兜底：新增（index < 0）与改名（isRename）都要查重。
                // 接口名是 FindApi（取第一条）/ Delete（删全部）/ 左栏定位的定位键，
                // 重复条目会导致「改的那条永不生效、删一次删两条」。
                // 界面层 TryValidate 是第一道，这里是最后一道——唯一性不该靠每个调用方记得查。
                bool isRename = index >= 0
                             && !MesApiDefinition.NameEquals(originalName, definition.Name);
                if ((index < 0 || isRename) && Find(snapshot, definition.Name) != null)
                {
                    lastSaveError = "接口名「" + definition.Name + "」已存在";
                    return false;
                }

                if (index >= 0)
                    apis[index] = definition.Clone();
                else
                    apis.Add(definition.Clone());

                return ReplaceSnapshot(snapshot, apis);
            }
        }

        public bool Delete(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            lock (writeLock)
            {
                lastSaveError = null;

                MesConfig snapshot = Current;
                List<MesApiDefinition> apis = snapshot.Apis.ToList();

                if (apis.RemoveAll(api => MesApiDefinition.NameEquals(api.Name, name)) == 0)
                {
                    lastSaveError = "接口不存在: " + name;
                    return false;
                }

                return ReplaceSnapshot(snapshot, apis);
            }
        }

        private static MesApiDefinition Find(MesConfig snapshot, string name)
        {
            return snapshot.Apis.FirstOrDefault(
                api => api != null && MesApiDefinition.NameEquals(api.Name, name));
        }

        private static int IndexOfName(List<MesApiDefinition> apis, string name)
        {
            if (string.IsNullOrEmpty(name))
                return -1;

            return apis.FindIndex(api => MesApiDefinition.NameEquals(api.Name, name));
        }

        /// <summary>落盘成功才换快照——先落盘后换引用，失败时内存保持原快照。</summary>
        private bool ReplaceSnapshot(MesConfig snapshot, List<MesApiDefinition> apis)
        {
            var next = new MesConfig { Version = snapshot.Version, Apis = apis };

            if (!store.Save(next))
                return false;

            Volatile.Write(ref current, next);
            return true;
        }
    }
}
