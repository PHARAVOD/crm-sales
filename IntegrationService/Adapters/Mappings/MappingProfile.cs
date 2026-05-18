using AutoMapper;
using IntegrationService.Models;

namespace IntegrationService.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // CartItem → OrderLine
        CreateMap<CartItemDto, OrderLine>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity > 0 ? src.Quantity : 1));
        
        // Cart → OrderRequest
        CreateMap<CartDto, OrderRequest>()
            .ForMember(dest => dest.OrderLines, opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.LeadId, opt => opt.Ignore())
            .ForMember(dest => dest.ContactId, opt => opt.Ignore());
        
        // OrderRequest → CreateDealRequest
        CreateMap<OrderRequest, CreateDealRequest>()
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.TotalAmount))
            .ForMember(dest => dest.Stage, opt => opt.MapFrom(src => "new"));
        
        // Lead → Contact (если нужно)
        CreateMap<LeadDto, CreateLeadRequest>().ReverseMap();
        
        // Deal → CreateTaskRequest
        CreateMap<DealDto, CreateTaskRequest>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => $"Обработать сделку #{src.Id}"))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => $"Сделка на сумму {src.Amount} руб"))
            .ForMember(dest => dest.DealId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.AssignedTo, opt => opt.MapFrom(src => "manager@crm.com"))
            .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => DateTime.Now.AddDays(3).ToString("yyyy-MM-dd")));
    }
}