using Content.Shared.Cargo.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.Economy;

/// <summary>
///     Salary of a job, the ID is the job ID.
/// </summary>
[Prototype]
public sealed partial class JobSalaryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public int Salary;

    /// <summary>
    ///     Station department account the salary is paid from and the department of the employee.
    ///     Without one the salary is paid by Nanotrasen itself.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype>? Department;
}
