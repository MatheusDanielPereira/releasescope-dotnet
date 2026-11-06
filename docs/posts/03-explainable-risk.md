# Post 3 — Explainable risk

A risk score without an explanation is just another magic number.

In ReleaseScope, every deployment score is built from explicit factors.

Risk-increasing signals include:

• Failed automated tests  
• Unresolved security findings  
• Performance regression  
• Recent incident history  
• Changes to critical components  
• Large change surface

Risk-reducing signals include:

• Strong automated-test coverage  
• A verified rollback plan

Each factor has a signed impact and a human-readable explanation.

That creates three useful properties:

1. Engineers can challenge the model.  
2. Auditors can reconstruct the decision.  
3. Teams can see which control would materially reduce the risk.

The current engine is intentionally deterministic. I want the policy to remain understandable before introducing probabilistic or AI-assisted recommendations.

Would you prefer a deterministic model you can audit, or a more accurate model that is harder to explain?

#softwarearchitecture #dotnet #explainableai #devsecops #engineering
