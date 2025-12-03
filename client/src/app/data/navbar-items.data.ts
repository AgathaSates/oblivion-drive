import { UserTypeModel } from '../components/modules/auth/models/auth.models';
import { NavbarItem } from '../models/navbar-item';

export const NAVBAR_ITEMS: readonly NavbarItem[] = [
  { name: 'Página Inicial', route: '/inicio', icon: 'home' },
  {
    name: 'Funcionários',
    route: '/funcionarios',
    icon: 'badge',
    allowedUserTypes: [UserTypeModel.Company],
  },

  { name: 'Clientes', route: '/clientes', icon: 'groups' },
  { name: 'Condutores', route: '/condutores', icon: 'sports_motorsports' },
  { name: 'Categorias', route: '/categorias', icon: 'category' },
  { name: 'Automóveis', route: '/automoveis', icon: 'directions_car' },
  { name: 'Planos', route: '/planos-de-cobranca', icon: 'request_quote' },
  { name: 'Serviços', route: '/servicos', icon: 'cleaning_services' },
  { name: 'Aluguéis', route: '/alugueis', icon: 'assignment' },
  { name: 'Combustível', route: '/combustiveis', icon: 'local_gas_station' },
  { name: 'Parceiros', route: '/parceiros', icon: 'handshake' },
  { name: 'Cupons', route: '/cupons', icon: 'local_offer' },

  {
    name: 'Meu perfil',
    route: '/meu-perfil',
    icon: 'person',
    allowedUserTypes: [UserTypeModel.Employee],
  },
];
