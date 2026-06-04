using Datacenter.Application.Common.Security;
using MediatR;

namespace Datacenter.Application.Features.Adjustments.Commands;

/// <summary>ลบรายการปรับปรุง (ทุกคนที่มีสิทธิ์ในบริษัทลบได้ + audit trail, ตาม req v11 #7)</summary>
public record DeleteAdjustmentEntryCommand(int Id, int ClientCompanyId)
    : IRequest, IRequireCompanyAccess;
