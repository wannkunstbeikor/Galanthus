project('Crypto', 'cpp',
  version : '0.1',
  default_options : ['warning_level=3', 'cpp_std=c++14'])

add_global_link_arguments(
  '-static-libgcc',
  '-static-libstdc++',
  '-static',
  language: 'cpp',
)

lib = shared_library('crypto',
           'lib.cpp',
           dependencies: dependency('openssl', modules: ['crypto'], static: true))

custom_target('final binary',
          depends : lib,
          input : lib,
          output : 'fake',
          command : ['cp', '@INPUT@', '../../Galanthus/ThirdParty'],
          build_by_default : true)
