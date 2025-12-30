using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;

namespace Shared.Classes.Entidades.ADM
{
    /// <summary>
    /// Entidade para configuração de menus do sistema
    /// </summary>
    public class MenuConfiguracao : BaseEntidade
    {
        /// <summary>
        /// Ícone do menu (Material Icons ou nome do ícone)
        /// </summary>
        [BsonElement("icone")]
        public string Icone { get; set; } = string.Empty;

        /// <summary>
        /// Label/Texto exibido no menu
        /// </summary>
        [BsonElement("label")]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Rota/caminho da navegação
        /// </summary>
        [BsonElement("route")]
        public string Route { get; set; } = string.Empty;

        /// <summary>
        /// Indica se o menu está desabilitado
        /// </summary>
        [BsonElement("disabled")]
        public bool Disabled { get; set; } = false;

        /// <summary>
        /// Ordem de exibição do menu
        /// </summary>
        [BsonElement("ordem")]
        public int Ordem { get; set; } = 0;

        /// <summary>
        /// ID do menu pai (null para menus de nível raiz)
        /// </summary>
        [BsonElement("menuPaiId")]
        public string? MenuPaiId { get; set; }

        /// <summary>
        /// Submenus/itens filhos deste menu
        /// </summary>
        [BsonElement("filhos")]
        public List<MenuConfiguracao>? Filhos { get; set; }
    }
}
