using AutoMapper;
using DAO_ReputationService.Models;
using Helpers.Models.DtoModels.ReputationDbDto;

namespace DAO_ReputationService.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<UserReputationHistory, UserReputationHistoryDto>();
            CreateMap<UserReputationHistoryDto, UserReputationHistory>();

            CreateMap<UserReputationStake, UserReputationStakeDto>();
            CreateMap<UserReputationStakeDto, UserReputationStake>();

        }
    }
}
