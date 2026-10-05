import type { PropsWithChildren } from "react";
import { ContributionProvider } from "@/components/contribution/ContributionContext";
import { InitiativeProvider } from "@/components/initiative/InitiativeContext";

/** Wraps the app in the shared Initiative and Contribution context providers. */
export function AppProviders({ children }: PropsWithChildren) {
  return (
    <InitiativeProvider>
      <ContributionProvider>{children}</ContributionProvider>
    </InitiativeProvider>
  );
}

export default AppProviders;
