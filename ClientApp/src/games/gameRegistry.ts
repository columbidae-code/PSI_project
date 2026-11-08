import type { ComponentType } from "react";
import SinglePlayer from "../pages/SinglePlayer";

export type SoloGameProps = {
  onBack: () => void;
};

type GameConfig = {
  description: string;
  SoloComponent?: ComponentType<SoloGameProps>;
};

export const gameRegistry: Record<string, GameConfig> = {
  Geografija: {
    description: "Šalys, miestai, vėliavos ir pasaulis",
    SoloComponent: SinglePlayer,
  },

  Istorija: {
    description: "Įvykiai, datos ir istorinės asmenybės, prisidėkit patys",
  },

  "Lietuvių kalba": {
    description: "Lietuvių kalbos kirčiavimo žinios, prisidėkit patys",
  },

  Matematika: {
    description: "Skaičiai, formulės ir loginiai uždaviniai, prisidėkit patys",
  },
};
