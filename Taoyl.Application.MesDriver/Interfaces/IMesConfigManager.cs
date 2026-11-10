using System.Collections.Generic;
using Taoyl.Application.MesDriver.Models;

namespace Taoyl.Application.MesDriver.Interfaces
{
    /// <summary>
    /// MES 配置管理端口。读操作驱动内部与界面共用；写操作（Save / Delete / Reload）供调试界面。
    /// </summary>
    public interface IMesConfigManager
    {
        /// <summary>当前配置快照。发布后冻结，不得修改；调用方取一次全程用一份。</summary>
        MesConfig Current { get; }

        /// <summary>启动加载失败描述（成功为 null）。</summary>
        string LastLoadError { get; }

        /// <summary>
        /// 最近一次保存 / 删除的失败描述（成功为 null）。
        /// 既覆盖落盘失败，也覆盖查重失败、条目不存在这类非 IO 失败。
        /// </summary>
        string LastSaveError { get; }

        /// <summary>从磁盘重载。主文件损坏自动回退备份目录最新可解析的一份。</summary>
        void Reload();

        /// <summary>按接口名取深拷贝，供编辑。不存在返回 null。</summary>
        MesApiDefinition GetApi(string name);

        /// <summary>所有接口名，按配置中的顺序。</summary>
        IReadOnlyList<string> GetApiNames();

        /// <summary>
        /// 新增或更新一条，落盘成功后热替换快照（失败返回 false 且不换快照）。
        /// originalName：null / 空 = 新增；传旧名 = 更新或改名（按旧名定位替换，不留残条）。
        /// 🔴 新增与改名都查重，重复名返回 false——接口名是 FindApi / Delete 的定位键，
        /// 重复条目会导致「改的那条永不生效、删一次删两条」。
        /// </summary>
        bool Save(string originalName, MesApiDefinition definition);

        /// <summary>按接口名删除并落盘。不存在或落盘失败返回 false。</summary>
        bool Delete(string name);
    }
}
