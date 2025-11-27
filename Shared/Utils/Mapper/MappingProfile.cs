using AutoMapper;
using Shared.Classes.Entidades.ADM;
using Shared.Classes.Entidades.Client;
using Shared.Classes.ModelView.ADM;
using Shared.Classes.ModelView.Client;

namespace Shared.Utils.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            #region ADMIN
            CreateMap<Empresa, EmpresaModel>().ReverseMap();
            CreateMap<Usuario, UsuarioModel>().ReverseMap();
            CreateMap<UsuarioEmpresa, UsuarioEmpresaModel>().ReverseMap();
            #endregion
            #region CLIENT
            CreateMap<Cliente, ClienteModel>().ReverseMap();
            CreateMap<ConfiguracaoIA, ConfiguracaoIAModel>().ReverseMap();
            #endregion

        }
    }
}
