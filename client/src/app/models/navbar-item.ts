import { UserTypeModel } from '../components/modules/auth/models/auth.models';

export interface NavbarItem {
  name: string;
  route: string;
  icon: string;

  allowedUserTypes?: UserTypeModel[];
}
