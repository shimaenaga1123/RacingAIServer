import sys
from racing_sdk import RacingClient, Agent, Action, Observation, RaceInfo, RaceResult


class FollowTrackAgent(Agent):
    def on_race_start(self, race: RaceInfo) -> None:
        pass

    def act(self, obs: Observation) -> Action:
        steering = -(1.2 * obs.track.heading_error + 0.25 * obs.track.lateral_offset)

        target_speed = 15.0
        current_speed = obs.player.speed

        if current_speed < target_speed:
            throttle = 1.0
            brake = 0.0
        else:
            throttle = 0.2
            brake = 0.4

        return Action(steering=steering, throttle=throttle, brake=brake)

    def on_race_finish(self, result: RaceResult) -> None:
        pass


if __name__ == "__main__":
    server = sys.argv[1] if len(sys.argv) > 1 else "https://localhost:5164"
    code = sys.argv[2] if len(sys.argv) > 2 else "123456"

    print(f"Connecting to {server} for competition {code}...")
    client = RacingClient(server)
    session = client.join(code=code, agent_name="PythonBot")
    session.run(FollowTrackAgent())
