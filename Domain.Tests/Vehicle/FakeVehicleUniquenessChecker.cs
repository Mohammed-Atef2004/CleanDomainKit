using Domain.Vehicles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Tests.Vehicle
{
    public class FakeVehicleUniquenessChecker : IVehicleUniquenessChecker
    {
        private readonly bool _isUnique;

        public FakeVehicleUniquenessChecker(bool isUnique)
        {
            _isUnique = isUnique;
        }

        public bool IsPlateUnique(VehiclePlateNumber plate)
            => _isUnique;
    }
}
