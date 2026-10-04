# Racing AI Python SDK

Python AI Agent SDK for the Autonomous Racing Platform.

## Quickstart

```python
from racing import RacingClient, Agent, Action, Observation

class MyAI(Agent):
    def act(self, obs: Observation) -> Action:
        # Simple track following
        steering = -(1.2 * obs.track.heading_error + 0.25 * obs.track.lateral_offset)
        throttle = 1.0 if obs.player.speed < 16.0 else 0.2
        brake = 0.5 if obs.player.speed > 20.0 else 0.0
        return Action(steering=steering, throttle=throttle, brake=brake)

if __name__ == "__main__":
    client = RacingClient("http://localhost:5000")
    session = client.join(code="482731", agent_name="MyAI")
    session.run(MyAI())
```
