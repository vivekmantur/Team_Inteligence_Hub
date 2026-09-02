import type { PropsWithChildren } from "react";
import { ContributionProvider } from "@/components/contribution/ContributionContext";
import { InitiativeProvider } from "@/components/initiative/InitiativeContext";

export function AppProviders({ children }: PropsWithChildren) {
  return (
    <InitiativeProvider>
      <ContributionProvider>{children}</ContributionProvider>
    </InitiativeProvider>
  );
}

export default AppProviders;
