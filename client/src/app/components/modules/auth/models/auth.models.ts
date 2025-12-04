export interface RegisterModel {
  userName: string;
  email: string;
  password: string;
}

export interface LoginModel {
  userName: string;
  password: string;
}

export interface AccessTokenModel {
  key: string;
  expiration: string;
  authenticatedUser: AuthenticatedUserModel;
}

export interface AuthenticatedUserModel {
  id: string;
  name: string;
  email: string;
  userType: UserTypeModel;
}

export enum UserTypeModel {
  Employee = 'Employee',
  Company = 'Company',
}
