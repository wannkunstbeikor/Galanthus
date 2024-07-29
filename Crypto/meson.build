project('Crypto', 'cpp',
  version : '0.1',
  default_options : ['warning_level=3', 'cpp_std=c++14'])

lib = shared_library('crypto',
           'lib.cpp',
           dependencies: dependency('openssl', modules: ['crypto']))

custom_target('final binary',
          depends : lib,
          input : lib,
          output : 'fake',
          command : ['cp', '@INPUT@', '../../Galanthus/ThirdParty'],
          build_by_default : true)
