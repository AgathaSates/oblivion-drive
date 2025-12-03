import { ReviewItem } from '../models/review-item';

export const REVIEW_ITEMS: readonly ReviewItem[] = [
  {
    id: 1,
    name: 'Camila',
    avatarUrl: 'review/woman-4.jpg',
    rating: 5,
    text:
      'Aluguei para trabalhar como motorista de app e o processo foi muito simples. ' +
      'Resolvi tudo online e peguei o carro no mesmo dia.',
    row: 'top',
  },
  {
    id: 2,
    name: 'Leandro',
    avatarUrl: 'review/man-2.jpg',
    rating: 4.5,
    text:
      'Escolhi o Plano Controlado e consigo prever bem os custos do mês. ' +
      'Quando passo um pouco do KM, a cobrança é clara e sem surpresa.',
    row: 'top',
  },
  {
    id: 3,
    name: 'Patrícia',
    avatarUrl: 'review/woman-1.jpg',
    rating: 5,
    text:
      'Usei o Plano Livre para uma viagem longa em família. Não precisei me preocupar ' +
      'com quilometragem e o carro estava impecável.',
    row: 'top',
  },
  {
    id: 4,
    name: 'Rogério',
    avatarUrl: 'review/man-1.jpg',
    rating: 5,
    text:
      'Atendimento rápido e suporte sempre que precisei. Acompanhar os aluguéis na plataforma ' +
      'facilita muito o dia a dia.',
    row: 'top',
  },
  {
    id: 5,
    name: 'Bianca',
    avatarUrl: 'review/woman-2.jpg',
    rating: 4.5,
    text:
      'Precisei de um carro por poucos dias e o Plano Diário encaixou perfeito. ' +
      'Ficou bem mais barato do que depender de corridas todo o tempo.',
    row: 'bottom',
  },
  {
    id: 6,
    name: 'Eduardo',
    avatarUrl: 'review/man-3.jpg',
    rating: 5,
    text:
      'Os carros são novos e bem cuidados. Como uso para trabalho, isso faz muita diferença ' +
      'para passar confiança aos passageiros.',
    row: 'bottom',
  },
  {
    id: 7,
    name: 'Simone',
    avatarUrl: 'review/woman-3.jpg',
    rating: 4.5,
    text:
      'Gostei da transparência nos valores e da praticidade para renovar o aluguel. ' +
      'Já indiquei a Oblivion Drive para outros colegas motoristas.',
    row: 'bottom',
  },
];
