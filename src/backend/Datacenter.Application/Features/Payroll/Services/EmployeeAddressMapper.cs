using Datacenter.Application.Features.Payroll.DTOs;
using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.Payroll.Services;

/// <summary>แปลงที่อยู่แยกช่อง (flat columns บน Employee) ↔ EmployeeAddressDto</summary>
public static class EmployeeAddressMapper
{
    public static EmployeeAddressDto ToDto(Employee e) => new(
        e.AddrBuilding, e.AddrRoomNo, e.AddrFloor, e.AddrVillage,
        e.AddrHouseNo, e.AddrMoo, e.AddrSoi, e.AddrYaek, e.AddrRoad,
        e.AddrSubDistrict, e.AddrDistrict, e.AddrProvince, e.AddrPostalCode);
}
