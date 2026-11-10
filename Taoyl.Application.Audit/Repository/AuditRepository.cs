using System;
using System.Collections.Generic;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Models.Entities;
using Taoyl.Application.DataBase;

namespace Taoyl.Application.Audit.Repository
{
    /// <summary>
    /// 审计写侧仓储实现（SqlSugar）。
    /// </summary>
    public class AuditRepository : IAuditRepository
    {
        private readonly SqlSugarHelper dbHelper;

        public AuditRepository(SqlSugarHelper dbHelper)
        {
            // 注入 SqlSugarHelper 而非 SqlSugarScope：前者 Db 是 Lazy，构造期不触发 DB 创建
            this.dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
        }

        /// <summary>头表 + 明细表同一事务写入；异常原样上抛（含回滚），由服务层决定降级。</summary>
        public void Insert(AuditOperationEntity header, List<AuditFieldChangeEntity> details)
        {
            if (header == null) throw new ArgumentNullException(nameof(header));

            var db = dbHelper.Db;

            db.Ado.BeginTran();
            try
            {
                // 头表主键是 long（bigint）——必须用 BigIdentity 版本，普通版返回 int，自增溢出即静默截断
                long operationId = db.Insertable(header).ExecuteReturnBigIdentity();

                if (details != null && details.Count > 0)
                {
                    foreach (var c in details) c.OperationId = operationId;
                    db.Insertable(details).ExecuteCommand();
                }

                db.Ado.CommitTran();
            }
            catch
            {
                db.Ado.RollbackTran();
                throw;
            }
        }
    }
}
