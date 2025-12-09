export interface PartnerDetailModel {
  id: string;
  name: string;
}

export interface GetAllPartnersResponseModel {
  quantity: number;
  partners: PartnerDetailModel[];
}

export interface RegisterPartnerRequestModel {
  name: string;
}

export interface RegisterPartnerResponseModel {
  createdSuccessfully: boolean;
  name: string;
}

export interface UpdatePartnerRequestModel {
  name: string;
}

export interface UpdatePartnerResponseModel {
  updatedSuccessfully: boolean;
  name: string;
}

export interface GetPartnerByIdResponseModel {
  id: string;
  name: string;
}

export interface DeletePartnerResponseModel {
  deletedSuccessfully: boolean;
  partnerId: string;
}
