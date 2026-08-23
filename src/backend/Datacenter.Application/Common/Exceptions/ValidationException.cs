using FluentValidation.Results;

namespace Datacenter.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("พบข้อผิดพลาดในการตรวจสอบข้อมูล")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    /// <summary>สร้างจากรายการข้อผิดพลาดที่เตรียมเอง (ใช้ตอนตรวจกฎที่ต้องอ่านข้อมูลประกอบ เช่น เกณฑ์รหัสผ่าน)</summary>
    public ValidationException(IDictionary<string, string[]> errors)
        : base("พบข้อผิดพลาดในการตรวจสอบข้อมูล")
        => Errors = errors;
}
