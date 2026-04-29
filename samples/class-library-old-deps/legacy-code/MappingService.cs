using System.Collections.Generic;
using AutoMapper;

namespace Contoso.Mapping
{
    public class MappingService
    {
        private readonly IMapper _mapper;

        public MappingService()
        {
            // AutoMapper 6.x static initialization — removed in 9+
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<CustomerDto, Customer>();
                cfg.CreateMap<Customer, CustomerDto>();
            });
            _mapper = Mapper.Instance;
        }

        public IEnumerable<Customer> ToEntities(IEnumerable<CustomerDto> dtos)
        {
            foreach (var d in dtos)
            {
                yield return _mapper.Map<Customer>(d);
            }
        }

        public CustomerDto ToDto(Customer entity)
        {
            return _mapper.Map<CustomerDto>(entity);
        }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }

    public class CustomerDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }
}
