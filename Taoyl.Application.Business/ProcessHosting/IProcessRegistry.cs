namespace Taoyl.Application.Business.ProcessHosting
{
    /// <summary>
    /// 流程注册器接口，负责初始化和注册所有流程实例。
    ///
    /// 位置说明（K3）：注册器要认识具体流程子类（从容器 Resolve 各子类并逐个 Register），
    /// 这份「名录知识」不属于零引用的 TaskProcess 基座，故契约与实现同留实现层。
    /// </summary>
    public interface IProcessRegistry
    {
        /// <summary>初始注册所有流程</summary>
        void Init();
    }
}
