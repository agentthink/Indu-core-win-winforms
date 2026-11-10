using System.ComponentModel;

namespace Taoyl.Application.Configuration.Models
{
    /// <summary>
    /// 设备基础运行参数配置。
    /// </summary>
    public class BaseConfig : ConfigBase
    {
        private double maxSpeed = 100.0;
        private double minTemperature = 0.0;
        private double maxTemperature = 200.0;
        private int timeoutMs = 5000;

        /// <summary>
        /// 设备最大运行速度。
        /// </summary>
        [DisplayName("最大速度")]
        public double MaxSpeed
        {
            get => maxSpeed;
            set => SetProperty(ref maxSpeed, value);
        }

        /// <summary>
        /// 设备允许的最低温度。
        /// </summary>
        [DisplayName("最低温度")]
        public double MinTemperature
        {
            get => minTemperature;
            set => SetProperty(ref minTemperature, value);
        }

        /// <summary>
        /// 设备允许的最高温度。
        /// </summary>
        [DisplayName("最高温度")]
        public double MaxTemperature
        {
            get => maxTemperature;
            set => SetProperty(ref maxTemperature, value);
        }

        /// <summary>
        /// 通信超时时间（毫秒）。
        /// </summary>
        [DisplayName("通信超时(ms)")]
        public int TimeoutMs
        {
            get => timeoutMs;
            set => SetProperty(ref timeoutMs, value);
        }
    }
}
