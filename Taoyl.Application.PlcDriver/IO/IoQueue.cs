using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Taoyl.Application.PlcDriver.IO
{
    /// <summary>
    /// IO单队列
    /// 职责：统一承载缓存轮询读与缓存写请求
    /// 去重规则：TryEnqueue 内部按 Kind+GroupName+TagName 仅对缓存读去重（同标签在队不重复入队）；写不去重；直接读写不入队
    /// </summary>
    public class IoQueue
    {
        private readonly BlockingCollection<IoRequest> _queue = new BlockingCollection<IoRequest>(10000);

        // 在队读请求去重标记（仅缓存读），IoWorker 单线程出队访问
        private readonly HashSet<string> _pendingReadKeys = new HashSet<string>();
        private readonly object _sync = new object();

        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="request">IO请求</param>
        /// <returns>true=入队成功，false=重复（仅读请求）或队列满</returns>
        public bool TryEnqueue(IoRequest request)
        {
            if (request == null)
                return false;

            // 缓存读去重：仅读请求，写不去重；直接读写不入队
            string key = null;
            if (request.Kind == IoRequestKind.Read)
            {
                key = GetKey(request.GroupName, request.TagName);
                lock (_sync)
                {
                    if (!_pendingReadKeys.Add(key))
                        return false; // 在队重复
                }
            }

            // 入队失败（队列满/已 CompleteAdding）回滚去重标记，避免 key 泄漏导致该标签后续读被永久去重
            // 注意：TryAdd 用 0 毫秒超时版——无参 TryAdd 在有界队列满时会无限阻塞，与"false=失败"语义不符
            bool added;
            try
            {
                added = _queue.TryAdd(request, 0);
            }
            catch (InvalidOperationException)
            {
                // CompleteAdding 后 TryAdd 抛异常（仅 Dispose 后可能触发）
                added = false;
            }

            if (!added)
            {
                if (key != null)
                {
                    lock (_sync)
                    {
                        _pendingReadKeys.Remove(key);
                    }
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// 出队（阻塞等待指定毫秒）
        /// </summary>
        /// <param name="request">出队的请求</param>
        /// <param name="millisecondsTimeout">等待毫秒数</param>
        /// <returns>true=取出成功</returns>
        public bool TryDequeue(out IoRequest request, int millisecondsTimeout)
        {
            if (_queue.TryTake(out request, millisecondsTimeout))
            {
                // 出队即执行，移除读去重标记
                if (request.Kind == IoRequestKind.Read)
                {
                    string key = GetKey(request.GroupName, request.TagName);
                    lock (_sync)
                    {
                        _pendingReadKeys.Remove(key);
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 当前队列深度
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// 标记完成（优雅关闭，仅 Dispose 调用；一次性终态，之后不可再入队）
        /// </summary>
        public void CompleteAdding()
        {
            _queue.CompleteAdding();
        }

        /// <summary>
        /// 清空队列（会话终结语义：Close 时调用，丢弃本会话残留的缓存读/写请求）
        /// 内容与去重标记一并清空（_sync 锁内，防 key 泄漏导致标签缓存读被永久去重）
        /// 注意：CompleteAdding 是终态不可逆，Close 后仍要重连复用，故清空走 Clear 而非 CompleteAdding
        /// </summary>
        public void Clear()
        {
            lock (_sync)
            {
                while (_queue.TryTake(out _))
                {
                }
                _pendingReadKeys.Clear();
            }
        }

        /// <summary>
        /// 去重键（组名+标签名）
        /// </summary>
        private static string GetKey(string groupName, string tagName)
        {
            return groupName + "\u0001" + tagName;
        }
    }
}
