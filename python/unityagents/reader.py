import os
import json
import numpy as np
import logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

from .brain import BrainInfo, BrainParameters
from .exception import UnityEnvironmentException, UnityActionException


class UnityReader(object):
	def __init__(self, academy_name, brain_names):   
	# need to figure out what to pass here. 
	# a brain could have multiple brain parameters accross sessions
		self._academy_name = academy_name
		self._brain_names = brain_names
		cwd = os.getcwd()

		if not isinstance(self._brain_names, list):
			self._brain_names = [self._brain_names]

		self._plays = []
		self._current_play = []
		previousParameters = None
		number_sessions = 0

		for brain_name in self._brain_names:
			file_path = os.path.join(cwd, "saved-plays", self._academy_name, brain_name)
			if not os.path.isfile(file_path):
				raise UnityBrainReaderException("There is no saved data for the brain {0} in the academy {1} ".format(brain_name, self._academy_name))



			with open(file_path, "r") as f :
				
				for l in f.readlines():
					try:
						x = json.loads(l)
					except:
						raise UnityBrainReaderException('''A line in the file {0} was not correct json :
							{1}'''.format(self._file_path, l))
					if 'timeStamp' in x:
						keep_session = self._valid_brain(x["brainParameters"])
						if (previousParameters is not None) and x["brainParameters"]!= previousParameters:
							raise UnityBrainReaderException('''The record {0} has different brain parameters compare to the other brains'''.format(brain_name))
						previousParameters = x["brainParameters"]
						brain = BrainParameters(brain_name, x["brainParameters"])
						number_sessions += 1
						# do if x is the right brain do, else, skip to the next session
					elif ('episodeCount' in x):
						if (len(self._current_play) > 0):
							self._plays.append(self._current_play)
							self._current_play = []
						# do append to the list of plays
					else:
						# try and if fail, discard whole episode (and log why...)
						try:
							agents = x["agents"]
							observations = self._reshape_observations(x["observations"], brain , len(agents))
							if (brain.state_space_type == "continuous"):
								states = np.array(x["state"]).reshape((len(agents), brain.state_space_size))
							else:
								states = np.array(state).reshape((len(agents), 1))
							memories = np.array(x["memory"]).reshape((len(agents),brain.memory_space_size))
							rewards = x["reward"]
							local_done = x["done"]
									# this is the point of difference with BrainInfo
							if (brain.action_space_type == "continuous"):
								actions = np.array(x["actions"]).reshape((len(agents), brain.action_space_size))
							else:
								actions = np.array(x["actions"]).reshape((len(agents), 1))
							self._current_play.append(
								BrainInfo(
									observations
									, states
									, memories
									, rewards
									, agents
									, local_done
									, actions)
								)


						except:
							raise
							self._current_play = []
					
			if len(self._current_play) > 0:
				self._plays.append(self._current_play)
			logger.info("\n{0} sessions loaded a total of {1} episodes.".format(number_sessions, len(self._plays))) 
				

	@property
	def plays(self):
		return self._plays

	def _valid_brain(self, brain_params):
		#TO DO
		return True

	def _reshape_observations(self, observation_list, brain_params, n_agents):
		result = []
		start_index = 0
		for obs_index in range(brain_params.number_observations):
			n_color = 1 if brain_params.camera_resolutions[obs_index]["blackAndWhite"] else 3
			h = brain_params.camera_resolutions[obs_index]["height"]  
			w = brain_params.camera_resolutions[obs_index]["width"] 
			end_index = start_index + (w * h * n_agents * n_color)
			result.append(np.array(
				observation_list[start_index : end_index]).reshape((n_agents, h, w, n_color  )) / 255.0)
			start_index = end_index

		return result
	



