using System.Diagnostics.CodeAnalysis;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.CouponModule;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Domain.RentalModule;
public class Rental : TenantEntity<Rental>
{
    public const decimal DefaultSecurityDepositAmount = 1000m;

    //----------------------------------------------------------

    // Cliente, Condutor, Veículo e Plano
    public Guid ClientId { get; private set; }
    public Client Client { get; private set; } = null!;

    public Guid DriverId { get; private set; }
    public Driver Driver { get; private set; } = null!;

    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    // Datas do aluguel
    public DateOnly StartDate { get; private set; }
    public DateOnly ExpectedReturnDate { get; private set; }
    public DateOnly? ActualReturnDate { get; private set; }

    // Tipo de plano
    public RentalPlanType PlanType { get; private set; }

    // Serviços
    private HashSet<Guid> _serviceIds = new();
    public IReadOnlyCollection<Guid> ServiceIds => _serviceIds;

    // Seguro
    public decimal InsuranceDailyPricePerPerson { get; private set; }
    public int InsurancePersonsCount { get; private set; }

    //----------------------------------------------------------

    // Quilometragem
    public int InitialOdometerInKm { get; private set; }
    public int? CurrentOdometerInKm { get; private set; }

    // Caução, danos e combustível
    public decimal SecurityDepositAmount { get; private set; }
    public bool HasDamage { get; private set; }
    public bool IsFuelTankFullOnReturn { get; private set; }

    // Cupom
    public Guid? CouponId { get; private set; }
    public Coupon? Coupon { get; private set; }

    // Status do aluguel
    public bool IsCompleted { get; private set; }

    // Valores
    public int? EstimatedTotalKilometers { get; private set; }

    public decimal RentalBasePrice { get; set; }      // Valor final de plano + km
    public decimal InsuranceTotalPrice { get; set; }  // Valor final de seguro
    public decimal ServicesTotalPrice { get; set; }   // Valor final de serviços
    public decimal EstimatedRentalAmount { get; set; } // valor previsto na criação

    public decimal CouponDiscountAmount { get; private set; }
    public decimal PenaltyPrice { get; set; }         // Valor final de multa
    public decimal FuelChargePrice { get; set; }      // Valor final de combustível
    public decimal GrossRentalAmount { get; set; }    // Soma de tudo
    public decimal FinalAmountToPay { get; set; }     // quanto ainda pagar após abater caução


    [ExcludeFromCodeCoverage]
    private Rental() { }

    public Rental(
        Guid companyId, Guid clientId, Guid driverId, Guid vehicleId,
        RentalPlanType planType,
        DateOnly startDate, DateOnly expectedReturnDate,
        decimal insuranceDailyPricePerPerson, int insurancePersonsCount,
        int estimatedTotalKilometers,
        decimal servicesTotalPrice, decimal insuranceTotalPrice, decimal rentalBasePrice, decimal estimatedRentalAmount,
        IEnumerable<Guid>? serviceIds = null
        )
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;

        ClientId = clientId;
        DriverId = driverId;
        VehicleId = vehicleId;

        PlanType = planType;

        StartDate = startDate;
        ExpectedReturnDate = expectedReturnDate;

        InsuranceDailyPricePerPerson = insuranceDailyPricePerPerson;
        InsurancePersonsCount = insurancePersonsCount;

        SecurityDepositAmount = DefaultSecurityDepositAmount;

        if (serviceIds is not null)
            SetServiceIds(serviceIds);

        IsCompleted = false;

        EstimatedTotalKilometers = estimatedTotalKilometers;

        ServicesTotalPrice = servicesTotalPrice;
        InsuranceTotalPrice = insuranceTotalPrice;
        RentalBasePrice = rentalBasePrice;
        EstimatedRentalAmount = estimatedRentalAmount;
    }

    public void CompleteReturn(
        DateOnly actualReturnDate, int initialOdometerInKm, int currentOdometerInKm,
        bool isFuelTankFullOnReturn,
        bool hasDamage,
        decimal rentalBasePrice, decimal insuranceTotalPrice, decimal servicesTotalPrice,
        decimal fuelChargePrice, decimal penaltyPrice, decimal grossRentalAmount,
        Guid? couponId, decimal couponDiscountAmount)
    {
        ActualReturnDate = actualReturnDate;

        InitialOdometerInKm = initialOdometerInKm;
        CurrentOdometerInKm = currentOdometerInKm;

        IsFuelTankFullOnReturn = isFuelTankFullOnReturn;
        HasDamage = hasDamage;

        RentalBasePrice = rentalBasePrice;
        InsuranceTotalPrice = insuranceTotalPrice;
        ServicesTotalPrice = servicesTotalPrice;
        FuelChargePrice = fuelChargePrice;
        PenaltyPrice = penaltyPrice;

        GrossRentalAmount = grossRentalAmount;

        if (HasDamage)
        {
            FinalAmountToPay = GrossRentalAmount;
        }
        else
        {
            decimal amountAfterDeposit = GrossRentalAmount - SecurityDepositAmount;

            FinalAmountToPay = amountAfterDeposit <= 0m ? 0m : amountAfterDeposit;
        }

        if (CouponDiscountAmount > 0m)
        {
            decimal amountAfterDiscount = FinalAmountToPay - CouponDiscountAmount;

            FinalAmountToPay = amountAfterDiscount <= 0m ? 0m : amountAfterDiscount;
        }

        IsCompleted = true;
    }

    public override void Update(Rental updatedEntity)
    {
        ClientId = updatedEntity.ClientId;
        DriverId = updatedEntity.DriverId;
        VehicleId = updatedEntity.VehicleId;

        PlanType = updatedEntity.PlanType;

        StartDate = updatedEntity.StartDate;
        ExpectedReturnDate = updatedEntity.ExpectedReturnDate;


        InsuranceDailyPricePerPerson = updatedEntity.InsuranceDailyPricePerPerson;
        InsurancePersonsCount = updatedEntity.InsurancePersonsCount;

        SetServiceIds(updatedEntity.ServiceIds);

        EstimatedTotalKilometers = updatedEntity.EstimatedTotalKilometers;

        ServicesTotalPrice = updatedEntity.ServicesTotalPrice;
        InsuranceTotalPrice = updatedEntity.InsuranceTotalPrice;
        RentalBasePrice = updatedEntity.RentalBasePrice;
        EstimatedRentalAmount = updatedEntity.EstimatedRentalAmount;
    }

    // Adiciona serviços à lista
    public void SetServiceIds(IEnumerable<Guid> serviceIds)
    {
        _serviceIds.Clear();

        foreach (Guid serviceId in serviceIds)
            _serviceIds.Add(serviceId);
    }
}