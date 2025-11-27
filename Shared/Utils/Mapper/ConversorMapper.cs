using AutoMapper;

namespace Shared.Utils.Mapper
{
    public class ConversorMap<TViewModel, TEntidade>(IMapper mapper) where TViewModel : class where TEntidade : class
    {
        private readonly IMapper _mapper = mapper;

        public TEntidade? ConverteViewModelParaEntidade(TViewModel objViewModel)
        {
            try
            {
                return _mapper.Map<TViewModel, TEntidade>(objViewModel);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Erro ao Converter ViewModel Para Entidade", ex);
            }
        }

        public TViewModel? ConverteEntidadeParaViewModel(TEntidade? objEntidade)
        {
            try
            {
                if (objEntidade != null)
                    return _mapper.Map<TEntidade, TViewModel>(objEntidade);
                return null;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Erro ao Converter Entidade Para ViewModel", ex);
            }
        }

        public IEnumerable<TViewModel>? ConverteListaEntidadesParaListaViewModels(IEnumerable<TEntidade>? entidades)
        {
            try
            {
                if (entidades != null)
                    return _mapper.Map<IEnumerable<TEntidade>, IEnumerable<TViewModel>>(entidades);
                return null;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Erro ao Converter Lista de Entidades Para ViewModels", ex);
            }
        }

        public IEnumerable<TEntidade>? ConverteListaViewModelsParaListaEntidades(IEnumerable<TViewModel>? viewModels)
        {
            try
            {
                if (viewModels != null)
                    return _mapper.Map<IEnumerable<TViewModel>, IEnumerable<TEntidade>>(viewModels);
                return null;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Erro ao Converter Lista de ViewModels Para Entidades", ex);
            }
        }


    }
}