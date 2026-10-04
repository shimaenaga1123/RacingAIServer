from abc import ABC, abstractmethod
from .models import Action, Observation, RaceInfo, RaceResult


class Agent(ABC):
    """Base class for all Python AI racing agents."""

    def on_race_start(self, race: RaceInfo) -> None:
        """Called once when a race is assigned and started."""
        pass

    @abstractmethod
    def act(self, observation: Observation) -> Action:
        """Called every simulation tick. Must return an Action."""
        raise NotImplementedError

    def on_race_finish(self, result: RaceResult) -> None:
        """Called when the race is finished."""
        pass
