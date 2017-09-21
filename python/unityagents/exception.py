class UnityEnvironmentException(Exception):
    """
    Related to errors starting and closing environment.
    """
    pass


class UnityActionException(Exception):
    """
    Related to errors with sending actions.
    """
    pass

class UnityReaderException(Exception):
    """
    Related to errors with reading data from file.
    """
    pass
