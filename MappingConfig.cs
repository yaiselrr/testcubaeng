using API.Models;
using API.Models.Dto;
using AutoMapper;

namespace API
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps() 
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                /*config.CreateMap<CreditCardUpdateDto, CreditCard>();
                config.CreateMap<CreditCard, CreditCardUpdateDto>();*/
            });

            return mappingConfig;

        }
    }
}
