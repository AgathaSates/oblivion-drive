// -------------------- Listagem ------------------------

export interface CouponDetailModel {
  id: string;
  name: string;
  value: number;
  expirationDate: string | Date;
  partnerId: string;
}

export interface GetAllCouponsResponseModel {
  quantity: number;
  coupons: CouponDetailModel[];
}

export interface CouponListItemViewModel extends CouponDetailModel {
  partnerName: string;
}

// -------------------- Cadastro ------------------------

export interface RegisterCouponRequestModel {
  name: string;
  value: number;
  expirationDate: string | Date;
  partnerId: string;
}

export interface RegisterCouponResponseModel {
  createdSuccessfully: boolean;
  name: string;
  value: number;
  expirationDate: string | Date;
  partnerId: string;
}

// -------------------- Edição ------------------------

export interface UpdateCouponRequestModel {
  name: string;
  value: number;
  expirationDate: string | Date;
  partnerId: string;
}

export interface UpdateCouponResponseModel {
  updatedSuccessfully: boolean;
  name: string;
  value: number;
  expirationDate: string | Date;
  partnerId: string;
}

// -------------------- Detalhes ------------------------

export interface GetCouponByIdResponseModel {
  id: string;
  name: string;
  value: number;
  expirationDate: string | Date;
  partnerId: string;
}

// -------------------- Exclusão ------------------------

export interface DeleteCouponResponseModel {
  deletedSuccessfully: boolean;
  couponId: string;
}
